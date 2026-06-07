namespace CoreBanking.Accounts.Domain.Interest;

/// <summary>A run of consecutive days with a constant end-of-day balance.</summary>
public sealed record DailyBalanceSpan(DateOnly From, int Days, decimal Balance);

/// <summary>
/// Builds end-of-day balance spans for a period from a transaction timeline
/// (Fineract EndOfDayBalance semantics: a transaction affects its own day's EOD balance).
/// </summary>
public static class InterestEngine
{
    public static IReadOnlyList<DailyBalanceSpan> BuildSpans(
        IEnumerable<SavingsAccountTransaction> transactions, DateOnly periodStart, DateOnly periodEnd)
    {
        var ordered = transactions
            .Where(t => t.TransactionDate <= periodEnd)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Sequence)
            .ToList();

        // Opening balance = running balance after the last transaction strictly before the period.
        var opening = 0m;
        foreach (var t in ordered)
            if (t.TransactionDate < periodStart) opening = t.RunningBalance;

        var spans = new List<DailyBalanceSpan>();
        var cursor = periodStart;
        var balance = opening;

        foreach (var group in ordered.Where(t => t.TransactionDate >= periodStart)
                     .GroupBy(t => t.TransactionDate))
        {
            if (group.Key > cursor)
            {
                spans.Add(new DailyBalanceSpan(cursor, group.Key.DayNumber - cursor.DayNumber, balance));
                cursor = group.Key;
            }
            balance = group.Last().RunningBalance; // groups preserve (date, sequence) source order
        }

        spans.Add(new DailyBalanceSpan(cursor, periodEnd.DayNumber - cursor.DayNumber + 1, balance));
        return spans;
    }
}
