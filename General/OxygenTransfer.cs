using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Oxygen.General
{
    internal sealed class OxygenTransfer
    {
        private const string Command = OxygenBase.modGUID + ".Tank.Command.v2";
        private const string State = OxygenBase.modGUID + ".Tank.State.v1";
        private const ulong None = ulong.MaxValue;
        private const float Interval = 0.1f, Lease = 0.35f;
        // Fixed interaction reach, shared by desktop and VR peers.
        private const float Reach = 2f;
        private readonly OxygenLedger ledger = new();
        private readonly Dictionary<ulong, BackpackGauge> backs = new();
        private readonly Dictionary<ulong, (ulong Target, float Seen, Vector3 Origin)> sessions = new();
        private readonly HashSet<ulong> activeDonors = new();
        private readonly OxygenPrediction prediction = new();
        private NetworkManager manager;
        private bool registered, applying, announced;
        private float announcedAmount;
        private ulong target = None, hovering = None;
        private float nextTick, lastTick;
        private bool held;
        internal Vector3? HoverPoint { get; private set; }

        internal bool ChangeLocal(float value)
        {
            if (applying || !registered || manager == null || !manager.IsListening) return false;
            if (!OxygenLedger.Finite(value)) return true;
            float delta = Mathf.Clamp01(value) - Mathf.Clamp01(OxygenInit.Percent);
            if (Mathf.Abs(delta) < 0.000001f) return true;
            ulong id = prediction.Record(delta);
            // Keep normal consumption/refill responsive; reconcile using acknowledged deltas.
            ApplyLocal(Mathf.Clamp01(OxygenInit.Percent + delta));
            Send(1, id, delta, None);
            return true;
        }

        private void ApplyLocal(float amount)
        {
            applying = true;
            try { OxygenInit.Percent = amount; }
            finally { applying = false; }
        }

        internal void Tick()
        {
            var network = NetworkManager.Singleton;
            if (network == null || !network.IsListening)
            {
                if (manager != null) EndSession();
                return;
            }
            if (manager != network) StartSession(network);
            var local = GameNetworkManager.Instance?.localPlayerController;
            if (local == null || !OxygenInit.IsOxygenHUDInitialized) return;
            if (!registered && Time.realtimeSinceStartup >= nextTick)
            {
                if (!announced) { announcedAmount = Mathf.Clamp01(OxygenInit.Percent); announced = true; }
                Send(0, 0, announcedAmount, None);
            }
            var action = InteractAction;
            // LCVR handles its grip action itself and bypasses the vanilla
            // Interact_performed callback. Start only on a new grip press.
            if (VrCompatibility.Active && action?.WasPressedThisFrame() == true && CanAct(local))
                BeginInteraction(local);
            bool pressed = CanAct(local) && action?.IsPressed() == true;
            if (!pressed && held) { target = None; Send(2, 0, 0, None); }
            held = pressed;
            if (target != None && (!CanAct(local) || !CanContinue(local, Player(target)) ||
                OxygenInit.Percent <= 0 || !ledger.Tanks.TryGetValue(target, out var recipient) || recipient.Amount >= 1))
                { target = None; Send(2, 0, 0, None); }
            if (Time.realtimeSinceStartup < nextTick) return;
            float now = Time.realtimeSinceStartup;
            // Preserve the configured rate between uneven frames without paying
            // back a long stall beyond the lifetime of a transfer intent.
            float dt = Mathf.Min(Lease, Mathf.Max(0, now - lastTick));
            lastTick = now;
            nextTick = now + Interval;
            if (target != None) Send(2, 0, 0, target);
            if (manager.IsServer)
            {
                foreach (var pair in new List<KeyValuePair<ulong, (ulong Target, float Seen, Vector3 Origin)>>(sessions))
                {
                    if (now - pair.Value.Seen > Lease || !CanContinueFrom(Player(pair.Key), Player(pair.Value.Target), pair.Value.Origin) ||
                        ledger.Transfer(pair.Key, pair.Value.Target,
                            Mathf.Clamp(OxygenBase.OxygenConfig.transferRate.Value, 0.001f, 1f) * dt) <= 0)
                        sessions.Remove(pair.Key);
                }
                Broadcast();
            }
            UpdateBacks();
        }

        private void StartSession(NetworkManager network)
        {
            EndSession();
            manager = network;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(Command, OnCommand);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(State, OnState);
            manager.OnClientDisconnectCallback += OnDisconnect;
            nextTick = lastTick = Time.realtimeSinceStartup;
        }

        private void EndSession()
        {
            if (manager != null)
            {
                manager.CustomMessagingManager?.UnregisterNamedMessageHandler(Command);
                manager.CustomMessagingManager?.UnregisterNamedMessageHandler(State);
                manager.OnClientDisconnectCallback -= OnDisconnect;
            }
            foreach (var back in backs.Values) if (back != null) UnityEngine.Object.Destroy(back.gameObject);
            backs.Clear(); ledger.Tanks.Clear(); sessions.Clear(); prediction.Clear(); activeDonors.Clear();
            registered = held = announced = false; target = hovering = None;
            manager = null;
        }

        private void OnDisconnect(ulong id)
        {
            if (id == manager.LocalClientId) { EndSession(); return; }
            ledger.Tanks.Remove(id); sessions.Remove(id);
            activeDonors.Remove(id);
            foreach (var donor in new List<ulong>(sessions.Keys))
                if (sessions[donor].Target == id) sessions.Remove(donor);
            if (backs.TryGetValue(id, out var back) && back != null) UnityEngine.Object.Destroy(back.gameObject);
            backs.Remove(id);
            if (target == id) target = None;
        }

        private void Send(byte kind, ulong sequence, float delta, ulong recipient)
        {
            var local = GameNetworkManager.Instance?.localPlayerController;
            var aim = kind == 2 && recipient != None && local?.gameplayCamera != null ?
                VrCompatibility.Aim(local) : default;
            if (manager.IsServer) { Handle(manager.LocalClientId, kind, sequence, delta, recipient, aim); return; }
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(kind); writer.WriteValueSafe(sequence);
            writer.WriteValueSafe(delta); writer.WriteValueSafe(recipient);
            writer.WriteValueSafe(aim.origin); writer.WriteValueSafe(aim.direction);
            manager.CustomMessagingManager.SendNamedMessage(Command, NetworkManager.ServerClientId,
                writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnCommand(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer) return;
            try
            {
                reader.ReadValueSafe(out byte kind); reader.ReadValueSafe(out ulong sequence);
                reader.ReadValueSafe(out float delta); reader.ReadValueSafe(out ulong recipient);
                reader.ReadValueSafe(out Vector3 origin); reader.ReadValueSafe(out Vector3 direction);
                if (reader.Position == reader.Length) Handle(sender, kind, sequence, delta, recipient, new Ray(origin, direction));
            }
            catch (Exception ex) { Configuration.OxygenConfig.mls.LogWarning($"Invalid tank command: {ex.Message}"); }
        }

        private void Handle(ulong sender, byte kind, ulong sequence, float delta, ulong recipient, Ray aim)
        {
            if (!manager.ConnectedClients.ContainsKey(sender) || Player(sender) == null) return;
            if (kind == 0) ledger.Register(sender, delta);
            else if (kind == 1) ledger.Change(sender, sequence, delta);
            else if (kind == 2)
            {
                if (recipient == None) { sessions.Remove(sender); return; }
                bool continuing = sessions.TryGetValue(sender, out var current) && current.Target == recipient &&
                    Time.realtimeSinceStartup - current.Seen <= Lease;
                if (ledger.Tanks.ContainsKey(sender) && ledger.Tanks.ContainsKey(recipient) &&
                    ValidAim(Player(sender), aim) && CanContinueFrom(Player(sender), Player(recipient), aim.origin) &&
                    (continuing || CanStartRay(Player(sender), Player(recipient), aim)))
                    sessions[sender] = (recipient, Time.realtimeSinceStartup, aim.origin);
                else sessions.Remove(sender);
            }
        }

        private void Broadcast()
        {
            foreach (var pair in ledger.Tanks)
            {
                ulong active = sessions.TryGetValue(pair.Key, out var session) ? session.Target : None;
                using var writer = new FastBufferWriter(32, Allocator.Temp);
                writer.WriteValueSafe(pair.Key); writer.WriteValueSafe(pair.Value.Amount);
                writer.WriteValueSafe(pair.Value.Revision); writer.WriteValueSafe(active);
                foreach (ulong client in manager.ConnectedClientsIds)
                    if (client != manager.LocalClientId)
                        manager.CustomMessagingManager.SendNamedMessage(State, client, writer, NetworkDelivery.ReliableSequenced);
                Accept(pair.Key, pair.Value.Amount, pair.Value.Revision, active);
            }
        }

        private void OnState(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != NetworkManager.ServerClientId) return;
            try
            {
                reader.ReadValueSafe(out ulong id); reader.ReadValueSafe(out float amount);
                reader.ReadValueSafe(out ulong ack); reader.ReadValueSafe(out ulong active);
                if (reader.Position == reader.Length && OxygenLedger.Finite(amount) && amount >= 0 && amount <= 1)
                    Accept(id, amount, ack, active);
            }
            catch (Exception ex) { Configuration.OxygenConfig.mls.LogWarning($"Invalid tank state: {ex.Message}"); }
        }

        private void Accept(ulong id, float amount, ulong ack, ulong active)
        {
            if (active != None) activeDonors.Add(id); else activeDonors.Remove(id);
            if (!manager.IsServer)
            {
                ledger.Register(id, amount);
                ledger.Tanks[id].Amount = amount;
                ledger.Tanks[id].Revision = ack;
            }
            if (id == manager.LocalClientId)
            {
                float initialDelta = !registered && announced ? Mathf.Clamp01(OxygenInit.Percent) - announcedAmount : 0;
                registered = true;
                ApplyLocal(prediction.Reconcile(amount, ack));
                if (Mathf.Abs(initialDelta) > 0.000001f) ChangeLocal(OxygenInit.Percent + initialDelta);
            }
            if (backs.TryGetValue(id, out var back) && back != null)
                back.SetState(amount, active != None);
        }

        private void UpdateBacks()
        {
            foreach (var pair in ledger.Tanks)
            {
                var player = Player(pair.Key);
                if (player == null) continue;
                if (!backs.TryGetValue(pair.Key, out var back) || back == null)
                    backs[pair.Key] = back = BackpackGauge.Create(player);
                back.SetState(pair.Value.Amount, activeDonors.Contains(pair.Key));
            }
        }

        internal void Hover(PlayerControllerB local)
        {
            if (local == null || !local.IsOwner) return;
            hovering = None;
            HoverPoint = null;
            if (manager == null || !registered || !CanAct(local)) return;
            if (target != None)
            {
                var recipient = Player(target);
                if (recipient != null) HoverPoint = BackpackGauge.InteractionBounds(recipient).center;
                local.cursorTip.text = "Transferring oxygen...";
                return;
            }
            foreach (var pair in ledger.Tanks)
                if (pair.Key != manager.LocalClientId && CanStart(local, Player(pair.Key)))
                {
                    HoverPoint = BackpackGauge.InteractionBounds(Player(pair.Key)).center;
                    if (pair.Value.Amount >= 1) { local.cursorTip.text = "Oxygen tank is full"; break; }
                    if (OxygenInit.Percent <= 0) { local.cursorTip.text = "Your oxygen tank is empty"; break; }
                    hovering = pair.Key;
                    local.cursorTip.text = "Transfer oxygen";
                    break;
                }
        }

        // PlayerControllerB subscribes to the global action asset. PlayerInput
        // may own a separate copy whose bindings differ after rebinding.
        private static InputAction InteractAction
        {
            get
            {
                var playerActions = IngamePlayerSettings.Instance?.playerInput?.actions;
                if (VrCompatibility.Active)
                    return playerActions?.FindAction("Controls/Interact") ?? playerActions?.FindAction("Interact");
                return InputSystem.actions?.FindAction("Interact") ?? playerActions?.FindAction("Interact");
            }
        }

        internal bool BeginInteraction(PlayerControllerB local)
        {
            if (manager == null || !registered || !local.IsOwner || !CanAct(local)) return false;
            if (target != None) return true;
            Hover(local); // Validate the current aim again when the input event fires.
            if (hovering == None) return false;
            target = hovering;
            Send(2, 0, 0, target);
            return true;
        }

        private static PlayerControllerB Player(ulong id)
        {
            var round = StartOfRound.Instance;
            if (round?.allPlayerScripts == null) return null;
            // A late joiner does not receive historical actualClientId assignments
            // for every existing peer. NGO ownership is replicated for all players.
            foreach (var player in round.allPlayerScripts)
                if (player != null && player.IsSpawned && player.OwnerClientId == id &&
                    (player.isPlayerControlled || player.isPlayerDead)) return player;
            return null;
        }

        private static bool CanAct(PlayerControllerB player) => player != null && player.isPlayerControlled &&
            !player.isPlayerDead && !player.inTerminalMenu && !player.isTypingChat && !player.isGrabbingObjectAnimation &&
            !player.inSpecialInteractAnimation && (!player.IsOwner || player.quickMenuManager == null || !player.quickMenuManager.isMenuOpen);

        private static bool CanContinue(PlayerControllerB from, PlayerControllerB to)
            => from?.gameplayCamera != null && CanContinueFrom(from, to, VrCompatibility.Aim(from).origin);

        private static bool CanContinueFrom(PlayerControllerB from, PlayerControllerB to, Vector3 origin)
        {
            if (!CanAct(from) || to == null || from == to || !to.isPlayerControlled || to.isPlayerDead ||
                Vector3.Distance(from.transform.position, to.transform.position) > Reach) return false;
            return ValidOrigin(from, origin) && Visible(from, to, origin, BackpackGauge.InteractionBounds(to).center);
        }

        private static bool CanStart(PlayerControllerB from, PlayerControllerB to)
            => from?.gameplayCamera != null && CanStartRay(from, to, VrCompatibility.Aim(from));

        private static bool CanStartRay(PlayerControllerB from, PlayerControllerB to, Ray aim)
        {
            if (!ValidAim(from, aim) || !CanContinueFrom(from, to, aim.origin)) return false;
            Vector3 offset = from.transform.position - to.transform.position;
            offset.y = 0;
            if (Vector3.Dot(offset.normalized, -to.transform.forward) < 0.5f) return false;
            // Client VR aim comes from the tracked hand. The host independently
            // bounds its origin, checks rear/range, intersects its own animated
            // tank bounds and raycasts its own scene; no client hit is trusted.
            return BackpackGauge.InteractionBounds(to).IntersectRay(aim, out float distance) &&
                distance <= Reach &&
                Visible(from, to, aim.origin, aim.GetPoint(distance));
        }

        private static bool ValidOrigin(PlayerControllerB from, Vector3 origin)
        {
            if (from == null || !Finite(origin)) return false;
            var offset = origin - from.transform.position;
            return offset.y >= -0.25f && offset.y <= 3.5f && offset.x * offset.x + offset.z * offset.z <= 2.25f;
        }

        private static bool ValidAim(PlayerControllerB from, Ray aim) => ValidOrigin(from, aim.origin) &&
            Finite(aim.direction) && aim.direction.sqrMagnitude >= 0.99f && aim.direction.sqrMagnitude <= 1.01f;

        private static bool Finite(Vector3 value) => OxygenLedger.Finite(value.x) &&
            OxygenLedger.Finite(value.y) && OxygenLedger.Finite(value.z);

        private static bool Visible(PlayerControllerB from, PlayerControllerB to, Vector3 origin, Vector3 point)
        {
            Vector3 direction = point - origin;
            foreach (var hit in Physics.RaycastAll(origin, direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(from.transform) && !hit.transform.IsChildOf(to.transform)) return false;
            return true;
        }
    }
}
