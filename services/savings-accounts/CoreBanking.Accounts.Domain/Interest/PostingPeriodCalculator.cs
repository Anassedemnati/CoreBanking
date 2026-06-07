namespace CoreBanking.Accounts.Domain.Interest;

/// <summary>
/// Splits a date range into calendar-aligned interest posting periods
/// (Fineract SavingsPostingInterestPeriodType, calendar variants only).
/// </summary>
public static class PostingPeriodCalculator
{
    /// <summary>
    /// Returns all complete periods starting from <paramref name="start"/> (inclusive)
    /// through <paramref name="asOf"/>. Only periods whose end date falls on or before
    /// <paramref name="asOf"/> are included.
    /// </summary>
    /// <remarks>Returns an empty list when <paramref name="start"/> is after <paramref name="asOf"/>.</remarks>
    public static IReadOnlyList<(DateOnly Start, DateOnly End)> Split(
        DateOnly start, DateOnly asOf, InterestPostingPeriod postingPeriod)
    {
        var result = new List<(DateOnly, DateOnly)>();
        var cursor = start;
        while (true)
        {
            var end = PeriodEnd(cursor, postingPeriod);
            if (end > asOf) break;
            result.Add((cursor, end));
            cursor = end.AddDays(1);
        }
        return result;
    }

    private static DateOnly PeriodEnd(DateOnly date, InterestPostingPeriod postingPeriod)
    {
        var endMonth = postingPeriod switch
        {
            InterestPostingPeriod.Monthly => date.Month,
            InterestPostingPeriod.Quarterly => ((date.Month - 1) / 3) * 3 + 3,
            InterestPostingPeriod.BiAnnual => date.Month <= 6 ? 6 : 12,
            InterestPostingPeriod.Annual => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(postingPeriod))
        };
        return new DateOnly(date.Year, endMonth, DateTime.DaysInMonth(date.Year, endMonth));
    }
}
