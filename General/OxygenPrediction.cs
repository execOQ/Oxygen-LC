using System.Collections.Generic;

namespace Oxygen.General
{
    // Reapply only changes the host has not acknowledged. A tank snapshot must never
    // restore oxygen already donated while an older local update was in flight.
    internal sealed class OxygenPrediction
    {
        private readonly Queue<KeyValuePair<ulong, float>> pending = new();
        internal ulong Revision { get; private set; }

        internal ulong Record(float delta)
        {
            pending.Enqueue(new KeyValuePair<ulong, float>(++Revision, delta));
            return Revision;
        }

        internal float Reconcile(float amount, ulong acknowledged)
        {
            while (pending.Count > 0 && pending.Peek().Key <= acknowledged) pending.Dequeue();
            foreach (var change in pending) amount = OxygenLedger.Clamp(amount + change.Value);
            return amount;
        }

        internal void Clear() { pending.Clear(); Revision = 0; }
    }
}
