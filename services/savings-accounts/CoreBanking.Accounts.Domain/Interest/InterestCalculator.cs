namespace CoreBanking.Accounts.Domain.Interest;

/// <summary>
/// Interest formulas ported from Fineract's EndOfDayBalance/CompoundingPeriod.
/// All math stays in decimal; the result is UNROUNDED — round only at posting time.
/// </summary>
public static class InterestCalculator
{
    public static decimal Calculate(
        IReadOnlyList<DailyBalanceSpan> spans,
        decimal nominalAnnualRatePercent,
        DaysInYearType daysInYear,
        InterestCompoundingPeriod compounding)
    {
        var dailyRate = nominalAnnualRatePercent / 100m / (int)daysInYear;

        if (compounding == InterestCompoundingPeriod.Daily)
        {
            // FV = PV·(1+r)^n done iteratively to stay in decimal (no Math.Pow round-trip).
            var accumulated = 0m;
            foreach (var span in spans)
                for (var d = 0; d < span.Days; d++)
                    accumulated += (span.Balance + accumulated) * dailyRate;
            return accumulated;
        }

        // Monthly compounding: simple interest within each calendar month;
        // interest accumulated in prior months joins the base at month boundaries.
        var total = 0m;
        foreach (var month in SplitByCalendarMonth(spans))
        {
            var monthBase = total;
            var monthInterest = 0m;
            foreach (var s in month)
                monthInterest += (s.Balance + monthBase) * dailyRate * s.Days;
            total += monthInterest;
        }
        return total;
    }

    private static IEnumerable<List<DailyBalanceSpan>> SplitByCalendarMonth(
        IReadOnlyList<DailyBalanceSpan> spans)
    {
        var flat = new List<DailyBalanceSpan>();
        foreach (var s in spans)
        {
            var from = s.From;
            var remaining = s.Days;
            while (remaining > 0)
            {
                var monthEnd = new DateOnly(from.Year, from.Month,
                    DateTime.DaysInMonth(from.Year, from.Month));
                var take = Math.Min(remaining, monthEnd.DayNumber - from.DayNumber + 1);
                flat.Add(new DailyBalanceSpan(from, take, s.Balance));
                from = from.AddDays(take);
                remaining -= take;
            }
        }
        return flat.GroupBy(s => (s.From.Year, s.From.Month)).Select(g => g.ToList());
    }
}
