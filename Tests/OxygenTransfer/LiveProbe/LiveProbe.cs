using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Bootstrap;
using GameNetcodeStuff;
using HarmonyLib;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

[BepInPlugin("oxygen.test.transfer", "Oxygen Transfer Live Probe", "1.0.0")]
[BepInDependency("consequential.Oxygen")]
public sealed class LiveProbe : BaseUnityPlugin
{
    private const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static LiveProbe instance;
    private string folder;
    private object transfers;
    private Type transferType;
    private PropertyInfo percent;
    private Vector3 position;
    private float yaw, next, release, aimHeight = 1.3f;
    private bool showcase;
    private Light showcaseLight;
    private bool posed, aim = true, pause;
    private bool aimGauge = true;
    private GameObject wall;
    private KeyControl key;
    private Gamepad gamepad;
    private bool gamepadInput;
    private GamepadButton padButton = GamepadButton.West;
    private InputActionAsset xrActions;
    private InputDevice xr;
    private bool xrInput;
    private InputActionAsset desktopActions;
    private AssetBundle xrBundle;
    private bool xrMode;
    private string xrButton = "gripButton";
    private object vrAimController;
    private Transform vrAimOriginal;
    private GameObject vrAimObject;
    private ulong? aimId;
    private GrabbableObject boost;
    private float boostStop, boostAdded;
    private Assembly oxygenAssembly;
    private IntPtr originalKeyboardLayout;
    [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint threadId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr LoadKeyboardLayout(string name, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr ActivateKeyboardLayout(IntPtr layout, uint flags);

    private void OnDestroy()
    {
        if (originalKeyboardLayout != IntPtr.Zero) ActivateKeyboardLayout(originalKeyboardLayout, 0);
    }
    private static object Get(object obj, string name)
    {
        if (obj == null) return null;
        for (Type type = obj.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, F);
            if (field != null) return field.GetValue(obj);
            var property = type.GetProperty(name, F);
            if (property != null) return property.GetValue(obj);
        }
        return null;
    }
    private static float Number(string text) => float.Parse(text, CultureInfo.InvariantCulture);
    private PlayerControllerB Local => GameNetworkManager.Instance?.localPlayerController;
    private PlayerControllerB Other => StartOfRound.Instance?.allPlayerScripts.FirstOrDefault(p => p != null && p.isPlayerControlled && p != Local && (!aimId.HasValue || p.OwnerClientId == aimId.Value));
    private static void Set(object obj, string name, object value)
    {
        for (Type type = obj.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, F);
            if (field != null) { field.SetValue(obj, value); return; }
        }
        throw new MissingFieldException(name);
    }

    private void Awake()
    {
        instance = this;
        folder = Directory.GetParent(Paths.BepInExRootPath).FullName;
        var plugin = Chainloader.PluginInfos["consequential.Oxygen"].Instance;
        oxygenAssembly = plugin.GetType().Assembly;
        transfers = Get(plugin, "Transfers"); transferType = transfers.GetType();
        percent = plugin.GetType().Assembly.GetType("Oxygen.General.OxygenInit").GetProperty("Percent", F);
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var harmony = new Harmony("oxygen.test.transfer");
        // DevelopmentStartup reloads saved fullscreen preferences after the
        // command-line window mode. Keep the isolated fixture windowed without
        // changing the user's stored settings or production Oxygen code.
        harmony.Patch(typeof(IngamePlayerSettings).GetMethod("LoadSettingsFromPrefs", F),
            postfix: new HarmonyMethod(typeof(LiveProbe), nameof(UseWindowedSettings)));
        harmony.Patch(typeof(IngamePlayerSettings).GetMethod("UpdateGameToMatchSettings", F),
            prefix: new HarmonyMethod(typeof(LiveProbe), nameof(UseWindowedSettings)));
        harmony.Patch(typeof(PlayerControllerB).GetMethod("SetHoverTipAndCurrentInteractTrigger", F),
            prefix: new HarmonyMethod(typeof(LiveProbe), nameof(PoseBeforeHover)));
        harmony.Patch(typeof(PlayerControllerB).GetMethod("LateUpdate", F),
            prefix: new HarmonyMethod(typeof(LiveProbe), nameof(PoseBeforeHover)));
        harmony.Patch(transferType.GetMethod("Tick", F), prefix: new HarmonyMethod(typeof(LiveProbe), nameof(BeforeTransferTick)));
        harmony.Patch(transferType.GetMethod("BeginInteraction", F), postfix: new HarmonyMethod(typeof(LiveProbe), nameof(AfterBeginInteraction)));
        var vrCompatibility = oxygenAssembly.GetType("Oxygen.General.VrCompatibility");
        if (vrCompatibility != null && !File.Exists(Path.Combine(folder, "native-vr.txt")))
            harmony.Patch(vrCompatibility.GetProperty("Active", F).GetGetMethod(true),
                postfix: new HarmonyMethod(typeof(LiveProbe), nameof(SimulateVrActivation)));
        harmony.Patch(typeof(NetworkManager).GetMethod("StartClient", F), prefix: new HarmonyMethod(typeof(LiveProbe), nameof(PrepareProxy)));
        harmony.Patch(oxygenAssembly.GetType("Oxygen.Items.OxyBoost").GetMethod("Update", F),
            prefix: new HarmonyMethod(typeof(LiveProbe), nameof(BeforeBoost)), postfix: new HarmonyMethod(typeof(LiveProbe), nameof(AfterBoost)));
        Log("READY");
    }

    private static void UseWindowedSettings(IngamePlayerSettings __instance)
    {
        foreach (string name in new[] { "settings", "unsavedSettings" })
        {
            var settings = Get(__instance, name);
            if (settings != null) Set(settings, "fullScreenType", FullScreenMode.Windowed);
        }
    }

    private static bool BeforeTransferTick() => !instance.pause;
    // Substitute only headset activation. Production action selection, input
    // handling, validation, HUD oxygen and networking remain unpatched.
    private static void SimulateVrActivation(ref bool __result) { if (instance.xrMode) __result = true; }
    private static void AfterBeginInteraction(PlayerControllerB local, bool __result)
    {
        if (local != null && local.IsOwner)
            instance.Log("BEGIN_INTERACTION success=" + __result + " target=" + Get(instance.transfers, "target"));
    }
    private static void BeforeBoost(object __instance, out float __state)
        => __state = ReferenceEquals(__instance, instance.boost) ? Convert.ToSingle(instance.percent.GetValue(null)) : -1;
    private static void AfterBoost(float __state)
    {
        if (__state >= 0) instance.boostAdded += Mathf.Max(0, Convert.ToSingle(instance.percent.GetValue(null)) - __state);
    }
    private static void PrepareProxy()
    {
        string path = Path.Combine(instance.folder, "proxy-port.txt");
        if (!File.Exists(path)) return;
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        var field = transport.GetType().GetField("ConnectionData", F);
        object data = field.GetValue(transport);
        data.GetType().GetField("Port", F).SetValue(data, ushort.Parse(File.ReadAllText(path).Trim()));
        data.GetType().GetField("Address", F).SetValue(data, "127.0.0.1");
        field.SetValue(transport, data);
        instance.Log("PROXY " + transport.GetType().FullName + " port=" + File.ReadAllText(path).Trim());
    }
    private static void PoseBeforeHover() => instance.LateUpdate();
    private void Log(string message) => File.AppendAllText(Path.Combine(folder, "transfer-events.log"), DateTime.UtcNow.ToString("O") + " " + message + "\n");
    private void Update()
    {
        try
        {
            if (release > 0 && Time.realtimeSinceStartup >= release) { Press(false); release = 0; Log("RELEASE"); }
            if (boostStop > 0 && Time.realtimeSinceStartup >= boostStop)
            {
                boost.ItemActivate(true, false); boostStop = 0; Log("BOOST_STOP");
            }
            string commandFile = Path.Combine(folder, "transfer-command.txt");
            if (File.Exists(commandFile))
            {
                string command;
                // A writer may still hold this diagnostic file. Retry next
                // frame rather than logging an expected sharing race as ERROR.
                try { command = File.ReadAllText(commandFile).Trim(); }
                catch (IOException) { return; }
                File.Delete(commandFile);
                foreach (string line in command.Split('\n')) Execute(line.Trim());
            }
            if (Time.realtimeSinceStartup < next) return;
            next = Time.realtimeSinceStartup + 0.1f;
            Snapshot();
        }
        catch (Exception ex) { Log("ERROR " + ex); }
    }

    private void LateUpdate()
    {
        var p = Local;
        if (!posed || p == null || p.isPlayerDead) return;
        p.transform.position = position;
        p.transform.rotation = Quaternion.Euler(0, yaw, 0);
        p.thisPlayerBody.eulerAngles = new Vector3(0, yaw, 0);
        if (aim && Other != null)
        {
            var tank = Other.GetComponentInChildren(oxygenAssembly.GetType("Oxygen.General.BackpackGauge"), true) as Component;
            var point = aimGauge && tank != null ? tank.transform.position :
                Other.transform.TransformPoint(new Vector3(0, aimHeight, -0.25f));
            var rotation = Quaternion.LookRotation(point - p.gameplayCamera.transform.position);
            p.thisPlayerBody.eulerAngles = new Vector3(0, rotation.eulerAngles.y, 0);
            p.GetType().GetField("cameraUp", F).SetValue(p, Mathf.DeltaAngle(0, rotation.eulerAngles.x));
            p.gameplayCamera.transform.rotation = rotation;
        }
        else
        {
            p.GetType().GetField("cameraUp", F).SetValue(p, 0f);
            p.gameplayCamera.transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
    }

    private void Execute(string command)
    {
        Log("COMMAND " + command);
        var args = command.Split(' ');
        switch (args[0])
        {
            case "pose":
                position = new Vector3(Number(args[1]), Number(args[2]), Number(args[3])); yaw = Number(args[4]);
                aim = args.Length < 6 || args[5] != "fixed"; posed = true;
                Local.isInElevator = false;
                Local.isInHangarShipRoom = false;
                Local.transform.SetParent(StartOfRound.Instance.playersContainer, true);
                Local.TeleportPlayer(position);
                if (!showcase && GameObject.Find("Oxygen test floor") == null)
                {
                    var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    floor.name = "Oxygen test floor";
                    floor.transform.position = new Vector3(1000, 9.5f, 1000);
                    floor.transform.localScale = new Vector3(20, 1, 20);
                }
                break;
            case "showcase":
                showcase = true;
                Screen.SetResolution(1920, 1080, false);
                var texture = IngamePlayerSettings.Instance.playerGameplayScreenTex;
                texture.Release(); texture.width = 1920; texture.height = 1080; texture.Create();
                Local.inTerminalMenu = Local.isTypingChat = false;
                Set(Local.quickMenuManager, "isMenuOpen", false);
                break;
            case "aimheight":
                aimGauge = args[1] == "gauge";
                if (!aimGauge) aimHeight = Number(args[1]); break;
            case "tickseconds":
                var cfg = Get(oxygenAssembly.GetType("Oxygen.OxygenBase").GetProperty("OxygenConfig", F).GetValue(null), "secTimer");
                cfg.GetType().GetProperty("LocalValue", F).SetValue(cfg, Number(args[1])); break;
            case "aimchecks": CheckAim(); break;
            case "chargercheck": CheckChargerVisuals(); break;
            case "english":
                if (originalKeyboardLayout == IntPtr.Zero) originalKeyboardLayout = GetKeyboardLayout(0);
                var english = LoadKeyboardLayout("00000409", 0);
                if (english == IntPtr.Zero || ActivateKeyboardLayout(english, 0) == IntPtr.Zero)
                    throw new InvalidOperationException("Could not activate English layout on game thread");
                InputSystem.QueueConfigChangeEvent(Keyboard.current);
                Log("KEYBOARD_LAYOUT " + GetKeyboardLayout(0).ToInt64().ToString("X"));
                break;
            case "hud": HUDManager.Instance.HideHUD(!bool.Parse(args[1])); break;
            case "land":
                if (!NetworkManager.Singleton.IsServer) throw new InvalidOperationException("Land from host");
                StartOfRound.Instance.currentLevel.currentWeather = LevelWeatherType.None;
                StartOfRound.Instance.overrideRandomSeed = true;
                StartOfRound.Instance.overrideSeedNumber = 24680;
                StartOfRound.Instance.StartGameServerRpc(); break;
            case "noon":
                TimeOfDay.Instance.globalTime = (TimeOfDay.Instance.totalTime * 0.35f - StartOfRound.Instance.currentLevel.OffsetFromGlobalTime) / StartOfRound.Instance.currentLevel.DaySpeedMultiplier;
                TimeOfDay.Instance.globalTimeSpeedMultiplier = 0;
                break;
            case "enter":
                posed = false;
                FindObjectsOfType<EntranceTeleport>().First(e => e.entranceId == 0 && e.isEntranceToBuilding).TeleportPlayer();
                break;
            case "inside": Local.isInsideFactory = bool.Parse(args[1]); break;
            case "free": posed = false; break;
            case "filllight":
                if (showcaseLight == null) showcaseLight = new GameObject("Showcase fill light").AddComponent<Light>();
                showcaseLight.type = LightType.Point;
                showcaseLight.range = 8;
                showcaseLight.intensity = Number(args[1]);
                showcaseLight.transform.position = Local.gameplayCamera.transform.position + Vector3.up * 0.6f + Local.transform.right * 0.7f;
                break;
            case "scout":
                var entrances = FindObjectsOfType<EntranceTeleport>().Select(e => new { e.name, e.entranceId, e.isEntranceToBuilding, position = e.entrancePoint.position.ToString("F3"), yaw = e.entrancePoint.eulerAngles.y });
                var lights = FindObjectsOfType<Light>().Where(l => l.enabled).Take(50).Select(l => new { l.name, type = l.type.ToString(), l.intensity, position = l.transform.position.ToString("F3") });
                File.WriteAllText(Path.Combine(folder, "scene.json"), JsonConvert.SerializeObject(new { level = StartOfRound.Instance.currentLevel.PlanetName, StartOfRound.Instance.shipHasLanded, position = Local.transform.position.ToString("F3"), entrances, lights }, Formatting.Indented));
                break;
            case "oxygen": percent.SetValue(null, Number(args[1])); break;
            case "target": aimId = ulong.Parse(args[1]); break;
            case "suit":
                var wearer = StartOfRound.Instance.allPlayerScripts.First(p => p != null && p.isPlayerControlled && p.OwnerClientId == ulong.Parse(args[1]));
                int suit = StartOfRound.Instance.unlockablesList.unlockables.FindIndex(u => u.unlockableName.Equals(args[2], StringComparison.OrdinalIgnoreCase));
                if (suit < 0) throw new InvalidOperationException("Suit not loaded: " + args[2]);
                typeof(UnlockableSuit).GetMethod("SwitchSuitForPlayer", F).Invoke(null, new object[] { wearer, suit, false }); break;
            case "hold":
                Press(false); gamepadInput = false; xrInput = false;
                key = InputSystem.actions.FindAction("Interact").controls.OfType<KeyControl>().First();
                Press(true); release = Time.realtimeSinceStartup + Number(args[1]);
                Log("HOLD key=" + key.path); break;
            case "gamepad":
                Press(false); gamepadInput = true; xrInput = false;
                gamepad = gamepad ?? InputSystem.AddDevice<Gamepad>();
                padButton = args.Length > 2 ? (GamepadButton)Enum.Parse(typeof(GamepadButton), args[2], true) : GamepadButton.West;
                Press(true); release = Time.realtimeSinceStartup + Number(args[1]);
                Log("HOLD gamepad=" + padButton); break;
            case "unplug":
                if (gamepad != null) InputSystem.RemoveDevice(gamepad);
                gamepad = null; release = 0; break;
            case "padbind":
                var padAction = InputSystem.actions.FindAction("Interact");
                padAction.Disable();
                foreach (int i in Enumerable.Range(0, padAction.bindings.Count).Where(i => padAction.bindings[i].path.StartsWith("<Gamepad>")))
                    padAction.ApplyBindingOverride(i, "<Gamepad>/" + args[1]);
                padAction.Enable(); break;
            case "xrassets":
                xrBundle = xrBundle ?? AssetBundle.LoadFromFile(Path.Combine(folder, "lethalcompanyvr"));
                File.WriteAllLines(Path.Combine(folder, "xr-assets.txt"), xrBundle.GetAllAssetNames()); break;
            case "xrprofile":
                Press(false); xrInput = false;
                xrActions = Instantiate(xrBundle.LoadAsset<InputActionAsset>(args[1]));
                File.WriteAllText(Path.Combine(folder, "xr-profile.json"), xrActions.ToJson());
                Log("XR_PROFILE " + xrActions.name); break;
            case "xrstart":
                desktopActions = IngamePlayerSettings.Instance.playerInput.actions;
                InputSystem.RegisterLayout("{\"name\":\"OxygenVirtualXR\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"gripButton\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"triggerButton\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"trigger\",\"layout\":\"Axis\",\"format\":\"FLT\"},{\"name\":\"triggerPressed\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"primaryButton\",\"layout\":\"Button\",\"format\":\"FLT\"}]}");
                xr = InputSystem.AddDevice("OxygenVirtualXR"); InputSystem.SetDeviceUsage(xr, "RightHand");
                IngamePlayerSettings.Instance.playerInput.neverAutoSwitchControlSchemes = true;
                IngamePlayerSettings.Instance.playerInput.actions = xrActions;
                xrActions = IngamePlayerSettings.Instance.playerInput.actions;
                // PlayerInput otherwise retains its desktop device filter.
                // A real XR loader pairs physical controllers; this fixture
                // instead exposes its one virtual controller to the asset.
                xrActions.bindingMask = null;
                xrActions.devices = new[] { xr };
                xrActions.Enable(); xrMode = true; break;
            case "xrhold":
                Press(false); gamepadInput = false; xrInput = true;
                xrButton = args.Length > 2 ? args[2] : "gripButton";
                Press(true); release = Time.realtimeSinceStartup + Number(args[1]); break;
            case "vrinput":
                // Keep LCVR's actual action asset and headset activation intact.
                xrActions = IngamePlayerSettings.Instance.playerInput.actions;
                File.WriteAllText(Path.Combine(folder, "native-vr-profile.json"), xrActions.ToJson());
                InputSystem.RegisterLayout("{\"name\":\"OxygenNativeVRButtons\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"gripButton\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"triggerButton\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"triggerPressed\",\"layout\":\"Button\",\"format\":\"FLT\"}]}");
                xr = InputSystem.AddDevice("OxygenNativeVRButtons"); InputSystem.SetDeviceUsage(xr, "RightHand");
                IngamePlayerSettings.Instance.playerInput.neverAutoSwitchControlSchemes = true;
                xrActions.devices = null;
                break;
            case "vrpoint":
                var vrAsm = Chainloader.PluginInfos["io.daxcess.lcvr"].Instance.GetType().Assembly;
                var nativeSession = (vrAsm.GetType("LCVR.Managers.VRSession") ?? vrAsm.GetType("LCVR.Player.VRSession"))
                    .GetProperty("Instance", F).GetValue(null);
                var nativePlayer = Get(nativeSession, "LocalPlayer");
                vrAimController = Get(nativePlayer, "PrimaryController");
                vrAimOriginal = Get(vrAimController, "InteractOrigin") as Transform;
                vrAimObject = new GameObject("Oxygen diagnostic hand ray");
                vrAimObject.transform.position = Local.gameplayCamera.transform.position + Vector3.down * 0.3f;
                var targetGauge = Other.GetComponentInChildren(oxygenAssembly.GetType("Oxygen.General.BackpackGauge"), true) as Component;
                vrAimObject.transform.LookAt(targetGauge.transform.position);
                vrAimController.GetType().GetProperty("InteractOrigin", F).SetValue(vrAimController, vrAimObject.transform);
                break;
            case "vrpointoff":
                if (vrAimController != null && vrAimOriginal != null)
                    vrAimController.GetType().GetProperty("InteractOrigin", F).SetValue(vrAimController, vrAimOriginal);
                if (vrAimObject != null) Destroy(vrAimObject);
                vrAimController = null; vrAimOriginal = null; vrAimObject = null;
                break;
            case "vrinputstop":
                Press(false); xrInput = false; release = 0;
                if (xr != null) InputSystem.RemoveDevice(xr); xr = null;
                IngamePlayerSettings.Instance.playerInput.neverAutoSwitchControlSchemes = false;
                break;
            case "vrmenu":
                var assembly = Chainloader.PluginInfos["io.daxcess.lcvr"].Instance.GetType().Assembly;
                if (args[1] == "lan")
                {
                    var init = FindObjectOfType(assembly.GetType("LCVR.Managers.InitSceneManager"));
                    init.GetType().GetMethod("ConfirmLocal", F).Invoke(init, null);
                }
                else if (args[1] == "host")
                {
                    var menu = FindObjectOfType<MenuManager>();
                    menu.ClickHostButton(); menu.ConfirmHostButton();
                }
                break;
            case "xrstop":
                Press(false); xrInput = false; xrMode = false; xrActions.Disable();
                IngamePlayerSettings.Instance.playerInput.actions = desktopActions;
                IngamePlayerSettings.Instance.playerInput.neverAutoSwitchControlSchemes = false;
                desktopActions.Enable();
                if (xr != null) InputSystem.RemoveDevice(xr); xr = null; break;
            case "xrunplug":
                if (xr != null) InputSystem.RemoveDevice(xr); xr = null; release = 0; break;
            case "release": Press(false); release = 0; break;
            case "pause": pause = bool.Parse(args[1]); break;
            case "terminal": Local.inTerminalMenu = bool.Parse(args[1]); break;
            case "chat": Local.isTypingChat = bool.Parse(args[1]); break;
            case "menu": Set(Local.quickMenuManager, "isMenuOpen", bool.Parse(args[1])); break;
            case "limited":
                StartOfRound.Instance.currentLevel.currentWeather = LevelWeatherType.Eclipsed;
                StartOfRound.Instance.inShipPhase = false; StartOfRound.Instance.shipIsLeaving = false;
                oxygenAssembly.GetType("Oxygen.General.WeatherHandler").GetMethod("RefreshLimit", F).Invoke(null, null); break;
            case "reserve":
                oxygenAssembly.GetType("Oxygen.Items.OxyCharger").GetProperty("RemainedOxygenAmount", F).SetValue(Charger(), Number(args[1])); break;
            case "refill":
                oxygenAssembly.GetType("Oxygen.Items.OxyCharger").GetMethod("FillOxygen", F).Invoke(Charger(), new object[] { false }); break;
            case "boostspawn":
                if (!NetworkManager.Singleton.IsServer) throw new InvalidOperationException("Spawn boost on test host");
                var plugin = Chainloader.PluginInfos["consequential.Oxygen"].Instance;
                var prefab = Get(Get(plugin, "oxyBoost"), "spawnPrefab") as GameObject;
                boost = Instantiate(prefab, Local.transform.position + Vector3.up, Quaternion.identity).GetComponent<GrabbableObject>();
                boost.GetComponent<NetworkObject>().SpawnWithOwnership(Local.actualClientId);
                boost.playerHeldBy = Local; boost.isHeld = true;
                Set(boost, "parentObject", Get(Local, "localItemHolder"));
                Local.ItemSlots[0] = boost; Local.currentItemSlot = 0;
                Local.currentlyHeldObjectServer = boost; Local.isHoldingObject = true;
                boost.EquipItem();
                // Match the game's pickup replication too: local inventory
                // assignment alone leaves remote playerHeldBy/previousPlayerHeldBy
                // unset and makes the base game's death/drop RPC fail later.
                typeof(PlayerControllerB).GetMethod("GrabObjectServerRpc", F).Invoke(Local,
                    new object[] { new NetworkObjectReference(boost.GetComponent<NetworkObject>()) });
                boostAdded = 0; break;
            case "boostuse": boost.ItemActivate(true, true); boostStop = Time.realtimeSinceStartup + Number(args[1]); break;
            case "die":
                // Lobby/orbit normally disallows death. Enable the game's real
                // death path only for this controlled final acceptance check.
                typeof(StartOfRound).GetField("allowLocalPlayerDeath", F).SetValue(StartOfRound.Instance, true);
                typeof(StartOfRound).GetField("timeSinceRoundStarted", F).SetValue(StartOfRound.Instance, 3f);
                typeof(StartOfRound).GetField("shipDoorsEnabled", F).SetValue(StartOfRound.Instance, true);
                Local.KillPlayer(Vector3.zero); break;
            case "crouch": typeof(PlayerControllerB).GetMethod("Crouch", F).Invoke(Local, new object[] { bool.Parse(args[1]) }); break;
            case "rebind":
                var action = InputSystem.actions.FindAction("Interact");
                action.Disable();
                int binding = Enumerable.Range(0, action.bindings.Count).First(i => action.bindings[i].path.StartsWith("<Keyboard>"));
                action.ApplyBindingOverride(binding, "<Keyboard>/" + args[1]);
                action.Enable(); break;
            case "wall":
                if (wall != null) Destroy(wall);
                if (args.Length > 1)
                {
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Oxygen live test obstacle";
                    wall.transform.position = new Vector3(Number(args[1]), Number(args[2]), Number(args[3]));
                    wall.transform.localScale = new Vector3(2, 3, 0.1f);
                }
                break;
            case "shot": ScreenCapture.CaptureScreenshot(Path.Combine(folder, args[1] + ".png")); break;
            case "mountcheck": StartCoroutine(CheckMount(Number(args[1]))); break;
            case "light":
                var light = new GameObject("Oxygen test light").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 3; light.transform.rotation = Quaternion.Euler(35, 150, 0); break;
            case "quit": Press(false); Application.Quit(); break;
            case "join": StartCoroutine(JoinWhenReady()); break;
        }
    }

    private IEnumerator JoinWhenReady()
    {
        float end = Time.realtimeSinceStartup + 120;
        MenuManager menu;
        while ((menu = FindObjectOfType<MenuManager>(true)) == null)
        {
            if (Time.realtimeSinceStartup >= end) { Log("JOIN_NOT_READY"); yield break; }
            yield return null;
        }
        menu.StartAClient();
        Log("JOIN_REQUESTED");
    }

    private void CheckChargerVisuals()
    {
        var charger = (Component)oxygenAssembly.GetType("Oxygen.Items.OxyCharger").GetProperty("Instance", F).GetValue(null);
        var prefab = (GameObject)Get(Chainloader.PluginInfos["consequential.Oxygen"].Instance, "oxyCharger");
        var stock = GameObject.Find("Environment/HangarShip/ScavengerModelSuitParts").transform.Find("Circle.002").GetComponentInChildren<Renderer>(true).sharedMaterial;
        var rows = charger.GetComponentsInChildren<MeshRenderer>(true).Select(r =>
        {
            var source = prefab.GetComponentsInChildren<MeshRenderer>(true).Single(p => p.name == r.name);
            var original = source.sharedMaterial;
            var material = r.sharedMaterial;
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            return new {
                renderer = r.name, shader = material.shader.name,
                usesGameShader = material.shader == stock.shader,
                replacesBundledShader = material.shader != original.shader,
                texturePreserved = material.GetTexture("_BaseColorMap") == original.GetTexture("_BaseColorMap"),
                colorPreserved = material.GetColor("_BaseColor") == original.GetColor("_BaseColor"),
                smoothnessPreserved = material.GetFloat("_Smoothness") == original.GetFloat("_Smoothness"),
                meshPreserved = mesh == source.GetComponent<MeshFilter>().sharedMesh,
                meshBounds = mesh.bounds.ToString(), worldBounds = r.bounds.ToString(),
                stereo = Local.gameplayCamera.stereoEnabled
            };
        }).ToArray();
        File.WriteAllText(Path.Combine(folder, "charger-visual-checks.json"), JsonConvert.SerializeObject(rows, Formatting.Indented));
        Log("CHARGER_VISUAL_CHECK " + JsonConvert.SerializeObject(rows));
    }

    private void CheckAim()
    {
        var p = Local; var other = Other;
        var gauge = other.GetComponentInChildren(oxygenAssembly.GetType("Oxygen.General.BackpackGauge"), true) as Component;
        var origin = p.gameplayCamera.transform.position;
        var ray = new Ray(origin, gauge.transform.position - origin);
        var method = transferType.GetMethod("CanStartRay", F);
        bool Allowed(Ray value) => (bool)method.Invoke(null, new object[] { p, other, value });
        var checks = new[] {
            new { name = "Actual visible gauge aim accepted", pass = Allowed(ray) },
            new { name = "Aim away rejected", pass = !Allowed(new Ray(origin, -ray.direction)) },
            new { name = "Zero direction rejected", pass = !Allowed(new Ray(origin, Vector3.zero)) },
            new { name = "Nonfinite origin rejected", pass = !Allowed(new Ray(new Vector3(float.NaN, origin.y, origin.z), ray.direction)) },
            new { name = "Remote origin spoof rejected", pass = !Allowed(new Ray(origin + Vector3.right * 10, ray.direction)) },
            new { name = "Excessive origin height rejected", pass = !Allowed(new Ray(origin + Vector3.up * 10, ray.direction)) }
        };
        File.WriteAllText(Path.Combine(folder, "aim-checks.json"), JsonConvert.SerializeObject(new { checks, aim = ray.ToString(), gauge = gauge.transform.position.ToString() }, Formatting.Indented));
    }

    private IEnumerator CheckMount(float seconds)
    {
        var baseline = new Dictionary<Transform, (Vector3 local, Quaternion rotation, Vector3 anchor, Quaternion anchorRotation)>();
        int frames = 0, samples = 0;
        float positionError = 0, angleError = 0, anchorTravel = 0, anchorTurn = 0;
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
        {
            // Sample after animation, player LateUpdate and rendering, rather
            // than accepting a pose that only matches earlier in the frame.
            yield return new WaitForEndOfFrame();
            var gauges = Get(transfers, "backs") as IDictionary;
            if (gauges == null) continue;
            frames++;
            foreach (object value in gauges.Values)
            {
                var gauge = (value as Component)?.transform;
                if (gauge == null || gauge.parent == null || gauge.parent.name != "spine.003") continue;
                var parent = gauge.parent;
                if (!baseline.TryGetValue(gauge, out var first))
                    baseline[gauge] = first = (gauge.localPosition, gauge.localRotation, parent.position, parent.rotation);
                positionError = Mathf.Max(positionError, Vector3.Distance(gauge.position, parent.TransformPoint(first.local)));
                angleError = Mathf.Max(angleError, Quaternion.Angle(gauge.rotation, parent.rotation * first.rotation));
                anchorTravel = Mathf.Max(anchorTravel, Vector3.Distance(parent.position, first.anchor));
                anchorTurn = Mathf.Max(anchorTurn, Quaternion.Angle(parent.rotation, first.anchorRotation));
                samples++;
            }
        }
        File.WriteAllText(Path.Combine(folder, "mount-check.json"), JsonConvert.SerializeObject(new {
            frames, samples, gauges = baseline.Count, maxPositionErrorMetres = positionError,
            maxAngleErrorDegrees = angleError, maxAnchorTravelMetres = anchorTravel, maxAnchorTurnDegrees = anchorTurn,
            pass = frames >= 30 && baseline.Count >= 2 && anchorTravel > 0.1f && anchorTurn > 10 && positionError < 0.001f && angleError < 0.1f
        }, Formatting.Indented));
    }

    private void Press(bool pressed)
    {
        if (xrInput && xr != null)
        {
            InputSystem.QueueDeltaStateEvent((AxisControl)xr[xrButton], pressed ? 1f : 0f);
            return;
        }
        if (gamepadInput && gamepad != null)
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(padButton, pressed));
            return;
        }
        if (key != null) InputSystem.QueueStateEvent((Keyboard)key.device, new KeyboardState(pressed ? new[] { key.keyCode } : Array.Empty<Key>()));
    }

    private Component Charger() => (Component)oxygenAssembly.GetType("Oxygen.Items.OxyCharger").GetProperty("Instance", F).GetValue(null);

    private void Snapshot()
    {
        var p = Local; var other = Other;
        if (posed && p != null && !p.isPlayerDead)
        {
            LateUpdate();
            // Position and view use the game's real network RPCs. Explicitly send
            // the locked pose, avoiding movement-threshold suppression in orbit.
            typeof(PlayerControllerB).GetMethod("UpdatePlayerPositionRpc", F).Invoke(p,
                new object[] { p.thisPlayerBody.localPosition, false, false, false, true });
            typeof(PlayerControllerB).GetMethod("UpdatePlayerRotationServerRpc", F).Invoke(p,
                new object[] { (short)Mathf.DeltaAngle(0, p.gameplayCamera.transform.localEulerAngles.x), (short)p.thisPlayerBody.eulerAngles.y });
        }
        var ledger = Get(Get(transfers, "ledger"), "Tanks") as IDictionary;
        var tanks = ledger?.Keys.Cast<object>().Select(id => new { id, amount = Get(ledger[id], "Amount"), revision = Get(ledger[id], "Revision") }).ToArray();
        bool eligible = p != null && other != null && (bool)transferType.GetMethod("CanStart", F).Invoke(null, new object[] { p, other });
        var gauges = Get(transfers, "backs") as IDictionary;
        var gaugeInfo = gauges?.Keys.Cast<object>().Select(id =>
        {
            var c = gauges[id] as Component;
            return new { id, position = c?.transform.position.ToString("F3"), parent = c?.transform.parent.name,
                anchorPosition = c?.transform.parent.position.ToString("F3"), forward = c?.transform.forward.ToString("F3"),
                pump = (Get(c, "pump") as AudioSource)?.isPlaying,
                renderers = c?.GetComponentsInChildren<Renderer>().Select(r => new { r.name, r.enabled, shader = r.sharedMaterial?.shader.name, bounds = r.bounds.ToString() }).ToArray() };
        }).ToArray();
        Chainloader.PluginInfos.TryGetValue("io.daxcess.lcvr", out var vrPlugin);
        var vrAssembly = vrPlugin?.Instance.GetType().Assembly;
        var vrType = vrAssembly?.GetType("LCVR.Managers.VRSession") ?? vrAssembly?.GetType("LCVR.Player.VRSession");
        var sessionObject = vrType?.GetProperty("Instance", F)?.GetValue(null);
        var controllerObject = Get(Get(sessionObject, "LocalPlayer"), "PrimaryController");
        var displays = new List<UnityEngine.XR.XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        var ray = p?.gameplayCamera != null ? (Ray)oxygenAssembly.GetType("Oxygen.General.VrCompatibility")
            .GetMethod("Aim", F).Invoke(null, new object[] { p }) : default;
        var targetBounds = other != null ? (Bounds)oxygenAssembly.GetType("Oxygen.General.BackpackGauge")
            .GetMethod("InteractionBounds", F).Invoke(null, new object[] { other }) : default;
        bool hitTank = targetBounds.IntersectRay(ray, out float tankDistance);
        var snapshot = new { time = Time.realtimeSinceStartup, server = NetworkManager.Singleton?.IsServer,
            oxygenTickSeconds = Get(Get(oxygenAssembly.GetType("Oxygen.OxygenBase").GetProperty("OxygenConfig", F).GetValue(null), "secTimer"), "Value"),
            vrControllerHover = Get(controllerObject, "IsHovering"),
            cursorActive = p?.cursorTip?.gameObject.activeInHierarchy, cursorEnabled = p?.cursorTip?.enabled,
            transferAimOrigin = ray.origin.ToString("F3"), transferAimDirection = ray.direction.ToString("F3"),
            targetBounds = targetBounds.ToString("F3"), hitTank, tankDistance,
            bodyDistance = p != null && other != null ? Vector3.Distance(p.transform.position, other.transform.position) : -1,
            rearCosine = p != null && other != null ? Vector3.Dot(Vector3.ProjectOnPlane(p.transform.position-other.transform.position, Vector3.up).normalized, -other.transform.forward) : -1,
            vrPluginVersion = vrPlugin?.Metadata.Version.ToString(),
            vrActiveNative = vrType?.GetProperty("InVR", F)?.GetValue(null),
            vrReadyNative = vrType?.GetProperty("Instance", F)?.GetValue(null) != null,
            xrDisplayRunning = displays.Any(d => d.running), stereoCamera = p?.gameplayCamera.stereoEnabled,
            startEligibility = StartOfRound.Instance?.allPlayerScripts.Where(a => a != null && a.isPlayerControlled)
                .SelectMany(a => StartOfRound.Instance.allPlayerScripts.Where(b => b != null && b.isPlayerControlled && b != a)
                    .Select(b => new { from = a.OwnerClientId, to = b.OwnerClientId,
                        allowed = (bool)transferType.GetMethod("CanStart", F).Invoke(null, new object[] { a, b }) })).ToArray(),
            inputBlocked = p == null || p.inTerminalMenu || p.isTypingChat || p.isGrabbingObjectAnimation || p.inSpecialInteractAnimation || (p.quickMenuManager != null && p.quickMenuManager.isMenuOpen),
            plugins = Chainloader.PluginInfos.Keys.ToArray(),
            suits = StartOfRound.Instance?.unlockablesList.unlockables.Where(u => u.suitMaterial != null).Select(u => u.unlockableName).ToArray(),
            wornSuits = StartOfRound.Instance?.allPlayerScripts.Where(player => player != null && player.isPlayerControlled)
                .Select(player => new { id = player.OwnerClientId, suit = Get(player, "currentSuitID") }).ToArray(),
            reserve = Get(Charger(), "RemainedOxygenAmount"), boostAdded, boostGas = Get(boost, "emittingGas"), boostFuel = Get(boost, "fuel"),
            boostHolders = FindObjectsOfType<GrabbableObject>().Where(item => item.GetType().FullName == "Oxygen.Items.OxyBoost")
                .Select(item => new { networkId = item.NetworkObjectId, holder = item.playerHeldBy != null ? (ulong?)item.playerHeldBy.OwnerClientId : null,
                    previousHolder = (Get(item, "previousPlayerHeldBy") as PlayerControllerB)?.OwnerClientId }).ToArray(),
            connected = NetworkManager.Singleton?.IsConnectedClient, id = p?.actualClientId, controlled = p?.isPlayerControlled, dead = p?.isPlayerDead,
            registered = Get(transfers, "registered"), amount = percent.GetValue(null), tanks, target = Get(transfers, "target"),
            sessionCount = (Get(transfers, "sessions") as IDictionary)?.Count,
            localPosition = p?.transform.position.ToString("F3"), remotePosition = other?.transform.position.ToString("F3"),
            bodyPosition = p?.thisPlayerBody.position.ToString("F3"), bodyName = p?.thisPlayerBody.name,
            localForward = p?.transform.forward.ToString("F3"), remoteForward = other?.transform.forward.ToString("F3"),
            camera = p?.gameplayCamera.transform.position.ToString("F3"), eligible, prompt = p?.cursorTip?.text,
            keyboardLayout = Keyboard.current?.keyboardLayout, interactKey = Keyboard.current?.eKey.displayName,
            usingController = StartOfRound.Instance?.localPlayerUsingController,
            xrMode,
            xrPressed = xrActions?.FindAction("Interact")?.IsPressed(),
            xrUsePressed = xrActions?.FindAction("Use")?.IsPressed(),
            xrActivatePressed = xrActions?.FindAction("ActivateItem")?.IsPressed(),
            xrControl = xrActions?.FindAction("Interact")?.activeControl?.path,
            xrControls = xrActions?.FindAction("Interact")?.controls.Select(c => c.path).ToArray(),
            xrBindingMask = xrActions?.bindingMask?.ToString(),
            actionControls = p != null ? InputSystem.actions?.FindAction("Interact")?.controls.Select(c => c.path).ToArray() : null,
            pressed = p != null ? InputSystem.actions?.FindAction("Interact")?.IsPressed() : null, gauges = gaugeInfo };
        string json = JsonConvert.SerializeObject(snapshot);
        File.WriteAllText(Path.Combine(folder, "transfer-state.json"), json);
        File.AppendAllText(Path.Combine(folder, "transfer-states.jsonl"), json + "\n");
    }
}
