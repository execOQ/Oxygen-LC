using System;
using System.Collections.Generic;

namespace Oxygen.General
{
    // All mutations and paired transfers run on the server's main thread.
    internal sealed class OxygenLedger
    {
        internal sealed class Tank
        {
            internal float Amount;
            internal ulong Revision;
        }
        internal readonly Dictionary<ulong, Tank> Tanks = new();
        internal static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        internal static float Clamp(float n) => Math.Max(0, Math.Min(1, n));

        internal bool Register(ulong id, float amount)
        {
            if (!Finite(amount) || Tanks.ContainsKey(id)) return false;
            Tanks.Add(id, new Tank { Amount = Clamp(amount) });
            return true;
        }

        internal bool Change(ulong id, ulong revision, float delta)
        {
            if (!Finite(delta) || Math.Abs(delta) > 1 || !Tanks.TryGetValue(id, out var tank) ||
                revision != tank.Revision + 1) return false;
            tank.Amount = Clamp(tank.Amount + delta);
            tank.Revision = revision;
            return true;
        }

        internal float Transfer(ulong donor, ulong recipient, float requested)
        {
            if (donor == recipient || !Finite(requested) || requested <= 0 ||
                !Tanks.TryGetValue(donor, out var from) || !Tanks.TryGetValue(recipient, out var to)) return 0;
            float amount = Math.Min(requested, Math.Min(from.Amount, 1 - to.Amount));
            from.Amount -= amount;
            to.Amount += amount;
            return amount;
        }
    }
}
