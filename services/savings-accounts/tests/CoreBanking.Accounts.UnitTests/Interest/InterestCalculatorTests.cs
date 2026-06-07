using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Interest;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Interest;

public sealed class InterestCalculatorTests
{
    // 5% / 365: dailyRate = 0.05/365

    [Fact]
    public void Simple_interest_single_span_monthly_compounding()
    {
        // 1000 for the 31 days of January: 1000 * (0.05/365) * 31 = 4.2465753...
        var spans = new[] { new DailyBalanceSpan(new DateOnly(2026, 1, 1), 31, 1000m) };

        var interest = InterestCalculator.Calculate(
            spans, 5.0m, DaysInYearType.Days365, InterestCompoundingPeriod.Monthly);

        interest.Should().BeApproximately(4.2466m, 0.0001m);
    }

    [Fact]
    public void Balance_change_mid_period_weights_spans_by_days()
    {
        // Jan 1-10 (10 days) at 1000, Jan 11-31 (21 days) at 1500
        var spans = new[]
        {
            new DailyBalanceSpan(new DateOnly(2026, 1, 1), 10, 1000m),
            new DailyBalanceSpan(new DateOnly(2026, 1, 11), 21, 1500m)
        };

        var interest = InterestCalculator.Calculate(
            spans, 5.0m, DaysInYearType.Days365, InterestCompoundingPeriod.Monthly);

        // 1000*r*10 + 1500*r*21 where r = 0.05/365  →  1.369863 + 4.315068 = 5.684931
        interest.Should().BeApproximately(5.6849m, 0.0001m);
    }

    [Fact]
    public void Monthly_compounding_adds_prior_month_interest_to_base()
    {
        // Quarterly posting period Jan-Mar, balance 1000 throughout, monthly compounding.
        var spans = new[] { new DailyBalanceSpan(new DateOnly(2026, 1, 1), 90, 1000m) };

        var interest = InterestCalculator.Calculate(
            spans, 5.0m, DaysInYearType.Days365, InterestCompoundingPeriod.Monthly);

        var r = 5.0m / 100m / 365m;
        var jan = 1000m * r * 31;
        var feb = (1000m + jan) * r * 28;
        var mar = (1000m + jan + feb) * r * 31;
        interest.Should().BeApproximately(jan + feb + mar, 0.000001m);
    }

    [Fact]
    public void Daily_compounding_compounds_each_day()
    {
        // 1000 for 10 days at 5%/365 daily compounding: (1+r)^10 - 1 applied to 1000 ≈ 1.37070
        var spans = new[] { new DailyBalanceSpan(new DateOnly(2026, 1, 1), 10, 1000m) };

        var interest = InterestCalculator.Calculate(
            spans, 5.0m, DaysInYearType.Days365, InterestCompoundingPeriod.Daily);

        interest.Should().BeApproximately(1.3707m, 0.0001m);
        // strictly more than simple interest 1.369863
        interest.Should().BeGreaterThan(1000m * (5.0m / 100m / 365m) * 10);
    }

    [Fact]
    public void Days360_uses_360_day_denominator()
    {
        var spans = new[] { new DailyBalanceSpan(new DateOnly(2026, 1, 1), 30, 1200m) };

        var interest = InterestCalculator.Calculate(
            spans, 6.0m, DaysInYearType.Days360, InterestCompoundingPeriod.Monthly);

        // 1200 * (0.06/360) * 30 = 6.00
        interest.Should().BeApproximately(6.0m, 0.0001m);
    }
}
