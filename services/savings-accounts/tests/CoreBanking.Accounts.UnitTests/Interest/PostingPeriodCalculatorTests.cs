using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Interest;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Interest;

public sealed class PostingPeriodCalculatorTests
{
    [Fact]
    public void Monthly_periods_end_on_last_day_of_each_month()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 31), InterestPostingPeriod.Monthly);

        periods.Should().Equal(
            (new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 31)),
            (new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)),
            (new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));
    }

    [Fact]
    public void Partial_trailing_period_is_excluded()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 27), InterestPostingPeriod.Monthly);

        periods.Should().Equal((new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void Quarterly_periods_end_on_calendar_quarter_ends()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 2, 10), new DateOnly(2026, 6, 30), InterestPostingPeriod.Quarterly);

        periods.Should().Equal(
            (new DateOnly(2026, 2, 10), new DateOnly(2026, 3, 31)),
            (new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)));
    }

    [Fact]
    public void Annual_period_ends_on_december_31()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 3, 1), new DateOnly(2027, 1, 15), InterestPostingPeriod.Annual);

        periods.Should().Equal((new DateOnly(2026, 3, 1), new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void No_complete_period_returns_empty()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 20), InterestPostingPeriod.Monthly);

        periods.Should().BeEmpty();
    }

    [Fact]
    public void BiAnnual_periods_end_on_june_and_december()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 5, 1), new DateOnly(2027, 6, 30), InterestPostingPeriod.BiAnnual);

        periods.Should().Equal(
            (new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30)),
            (new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31)),
            (new DateOnly(2027, 1, 1), new DateOnly(2027, 6, 30)));
    }

    [Fact]
    public void Annual_multi_year_span_produces_one_period_per_year()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 3, 1), new DateOnly(2027, 12, 31), InterestPostingPeriod.Annual);

        periods.Should().Equal(
            (new DateOnly(2026, 3, 1), new DateOnly(2026, 12, 31)),
            (new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31)));
    }

    [Fact]
    public void Monthly_period_ends_on_feb_29_in_leap_year()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29), InterestPostingPeriod.Monthly);

        periods.Should().Equal((new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29)));
    }
}
