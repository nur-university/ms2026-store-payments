using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Payments.Domain.Payments;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Payments.Infrastructure.Observability;

/// <summary>
/// Business metrics of the cashier's desk (meter <c>Store.Payments</c>). Taken from the status changes the unit
/// of work is about to persist and recorded only after <c>SaveChanges</c> succeeded.
/// </summary>
internal static class PaymentsMetrics
{
    public const string MeterName = "Store.Payments";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Transitions = Meter.CreateCounter<long>("store.payments.transitions",
        description: "Charge order status changes; previous_status is 'none' for a newly created one.");

    private static readonly Counter<long> Collected = Meter.CreateCounter<long>("store.payments.collected",
        description: "Charge orders collected at the cashier, by payment method.");

    private static readonly Counter<double> CollectedAmount = Meter.CreateCounter<double>("store.payments.collected.amount",
        description: "Money collected at the cashier, by payment method (invoice currency).");

    private static readonly Histogram<double> CollectionTime = Meter.CreateHistogram<double>("store.payments.collection_time",
        unit: "s", description: "Time a charge order waited at the cashier until it was collected.");

    internal readonly record struct Transition(string From, string To, string? Method, decimal Amount, double? WaitSeconds);

    /// <summary>Reads the status changes staged in the change tracker. Never throws: metrics must not break a commit.</summary>
    public static IReadOnlyList<Transition> Capture(ChangeTracker tracker)
    {
        var transitions = new List<Transition>();
        try
        {
            foreach (EntityEntry<Payment> entry in tracker.Entries<Payment>())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                {
                    continue;
                }

                PaymentStatus? from = entry.State == EntityState.Added
                    ? null
                    : (PaymentStatus)entry.Property(nameof(Payment.Status)).OriginalValue!;
                Payment payment = entry.Entity;

                if (from == payment.Status)
                {
                    continue;
                }

                bool collected = payment.Status == PaymentStatus.Completed;
                transitions.Add(new Transition(
                    from?.ToString() ?? "none",
                    payment.Status.ToString(),
                    collected ? payment.Method?.ToString() ?? "Unknown" : null,
                    payment.Amount,
                    collected && payment.PaidOn.HasValue
                        ? Math.Max(0, (payment.PaidOn.Value - payment.CreatedOn).TotalSeconds)
                        : null));
            }
        }
        catch
        {
            transitions.Clear();
        }

        return transitions;
    }

    public static void Record(IReadOnlyList<Transition> transitions)
    {
        foreach (Transition t in transitions)
        {
            Transitions.Add(1, new TagList { { "status", t.To }, { "previous_status", t.From } });

            if (t.Method != null)
            {
                var method = new KeyValuePair<string, object?>("method", t.Method);
                Collected.Add(1, method);
                CollectedAmount.Add((double)t.Amount, method);
                if (t.WaitSeconds.HasValue)
                {
                    CollectionTime.Record(t.WaitSeconds.Value, method);
                }
            }
        }
    }
}
