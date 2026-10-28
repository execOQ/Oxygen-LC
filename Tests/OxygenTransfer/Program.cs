using System;
using System.Linq;
using Oxygen.General;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
    private static bool Near(float a, float b) => Math.Abs(a - b) < 0.00001f;

    private static void Main()
    {
        var ledger = new OxygenLedger();
        Check(ledger.Register(0, 0.6f) && ledger.Register(1, 0.95f), "Host and remote tank registration");
        Check(!ledger.Register(0, 1) && Near(ledger.Tanks[0].Amount, 0.6f), "Repeated hello cannot refill a tank");
        float total = ledger.Tanks[0].Amount + ledger.Tanks[1].Amount;
        Check(Near(ledger.Transfer(0, 1, 0.5f), 0.05f), "Recipient capacity limits transfer");
        Check(Near(ledger.Tanks[0].Amount + ledger.Tanks[1].Amount, total), "Transfer conserves oxygen");
        Check(ledger.Transfer(0, 1, 0.1f) == 0, "Full tank stops transfer");
        Check(ledger.Transfer(0, 0, 1) == 0 && ledger.Transfer(0, 99, 1) == 0, "Self and missing players rejected");
        Check(ledger.Change(0, 1, -1), "Consumption reaches zero");
        Check(ledger.Transfer(0, 1, 0.1f) == 0, "Empty donor cannot transfer");
        Check(!ledger.Change(0, 1, 1) && !ledger.Change(0, 3, 1), "Duplicate and skipped revisions rejected");
        Check(ledger.Change(0, 2, 0.25f) && Near(ledger.Tanks[0].Amount, 0.25f), "Next revision can refill normally");
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Check(!ledger.Register(99, bad), "Nonfinite initial state rejected");
            Check(!ledger.Change(0, 3, bad), "Nonfinite mutation rejected");
            Check(ledger.Transfer(1, 0, bad) == 0, "Nonfinite transfer rejected");
        }
        Check(!ledger.Change(0, 3, 2) && ledger.Transfer(1, 0, -1) == 0, "Invalid magnitude rejected");

        // Competing donors cannot overfill a recipient, and competing recipients
        // cannot withdraw more than one donor holds.
        ledger = new OxygenLedger();
        ledger.Register(0, 0.08f); ledger.Register(1, 0); ledger.Register(2, 0);
        Check(Near(ledger.Transfer(0, 1, 0.05f), 0.05f) && Near(ledger.Transfer(0, 2, 0.05f), 0.03f),
            "Concurrent recipients share the available donor oxygen");
        Check(Near(ledger.Tanks.Values.Sum(x => x.Amount), 0.08f), "Concurrent withdrawals conserve oxygen");
        ledger = new OxygenLedger();
        ledger.Register(0, 0.6f); ledger.Register(1, 0.6f); ledger.Register(2, 0.97f);
        Check(Near(ledger.Transfer(0, 2, 0.05f), 0.03f) && ledger.Transfer(1, 2, 0.05f) == 0,
            "Concurrent donors share recipient headroom");

        // A delayed update/ack must not undo a transfer or double-apply local consumption.
        var prediction = new OxygenPrediction();
        Check(prediction.Record(-0.1f) == 1 && prediction.Record(0.02f) == 2, "Local mutation IDs are ordered");
        Check(Near(prediction.Reconcile(0.45f, 0), 0.37f), "In-flight consumption reapplied after donation snapshot");
        Check(Near(prediction.Reconcile(0.35f, 1), 0.37f), "Partial acknowledgement retains only pending refill");
        Check(Near(prediction.Reconcile(0.37f, 2), 0.37f), "Full acknowledgement removes prediction");
        Check(Near(prediction.Reconcile(0.32f, 2), 0.32f), "Later transfer snapshot does not replay old changes");
        prediction.Record(-1); prediction.Record(0.2f);
        Check(Near(prediction.Reconcile(0.1f, 2), 0.2f), "Prediction clamps each pending operation in order");
        prediction.Clear();
        Check(prediction.Record(-0.1f) == 1 && Near(prediction.Reconcile(1, 0), 0.9f), "New session clears old prediction");

        ledger = new OxygenLedger();
        var random = new Random(409);
        for (ulong i = 0; i < 8; i++) ledger.Register(i, (float)random.NextDouble());
        double original = ledger.Tanks.Values.Sum(x => (double)x.Amount);
        for (int i = 0; i < 100000; i++)
        {
            ulong from = (ulong)random.Next(8), to = (ulong)random.Next(8);
            ledger.Transfer(from, to, (float)random.NextDouble() * 0.05f);
            if (i % 1000 != 0) continue;
            Check(ledger.Tanks.Values.All(x => x.Amount >= 0 && x.Amount <= 1), "Random concurrent transfers stay within bounds");
            Check(Math.Abs(ledger.Tanks.Values.Sum(x => (double)x.Amount) - original) < 0.0001,
                "Random concurrent transfers conserve total oxygen");
        }
        Console.WriteLine($"Passed {checks} oxygen transfer checks (including 100,000 competing transfers).");
    }
}
