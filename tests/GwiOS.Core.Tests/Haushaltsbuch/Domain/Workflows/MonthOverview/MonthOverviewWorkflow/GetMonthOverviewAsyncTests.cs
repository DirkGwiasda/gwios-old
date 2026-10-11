using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Workflows.MonthOverview.MonthOverviewWorkflow;

/// <summary>
/// Covers <c>MonthOverviewWorkflow.GetMonthOverviewAsync</c>.
/// </summary>
public sealed class GetMonthOverviewAsyncTests
{
    private static readonly DateOnly October = new(2026, 10, 1);

    private readonly MonthOverviewWorkflowProbe _probe = new();

    [Fact]
    public async Task SumsEachCategoryOnItsOwnAndRolledUpWithItsSubCategories()
    {
        _probe.AddBooking(_probe.Household, 10m, new DateOnly(2026, 10, 2));
        _probe.AddBooking(_probe.Groceries, 40m, new DateOnly(2026, 10, 3));
        _probe.AddBooking(_probe.Groceries, 7.5m, new DateOnly(2026, 10, 4));
        _probe.AddBooking(_probe.Organic, 2.5m, new DateOnly(2026, 10, 5));

        MonthOverviewData overview = await GetOctoberAsync();

        MonthOverviewNode household = Assert.Single(overview.Expenses);
        Assert.Equal((10m, 60m, 1), (household.OwnAmount, household.TotalAmount, household.BookingCount));
        MonthOverviewNode groceries = Assert.Single(household.Children);
        Assert.Equal((47.5m, 50m, 2), (groceries.OwnAmount, groceries.TotalAmount, groceries.BookingCount));
        MonthOverviewNode organic = Assert.Single(groceries.Children);
        Assert.Equal((2.5m, 2.5m, 1), (organic.OwnAmount, organic.TotalAmount, organic.BookingCount));
        Assert.Empty(organic.Children);
    }

    [Fact]
    public async Task KeepsAParentWithoutOwnBookingsForItsSubCategories()
    {
        _probe.AddBooking(_probe.Organic, 2.5m, new DateOnly(2026, 10, 5));

        MonthOverviewData overview = await GetOctoberAsync();

        MonthOverviewNode household = Assert.Single(overview.Expenses);
        Assert.Equal((0m, 2.5m, 0), (household.OwnAmount, household.TotalAmount, household.BookingCount));
        Assert.Equal(_probe.Groceries, Assert.Single(household.Children).Category);
    }

    [Fact]
    public async Task SplitsIncomeAndExpensesAndLeavesOutCategoriesWithoutBookings()
    {
        _probe.AddBooking(_probe.Salary, 3000m, new DateOnly(2026, 10, 1));
        _probe.AddBooking(_probe.Mobility, 60m, new DateOnly(2026, 10, 9));

        MonthOverviewData overview = await GetOctoberAsync();

        Assert.Equal([_probe.Salary], overview.Incomes.Select(node => node.Category));
        Assert.Equal([_probe.Mobility], overview.Expenses.Select(node => node.Category));
    }

    [Fact]
    public async Task OrdersCategoriesByName()
    {
        _probe.AddBooking(_probe.Mobility, 60m, new DateOnly(2026, 10, 9));
        _probe.AddBooking(_probe.Household, 10m, new DateOnly(2026, 10, 2));

        MonthOverviewData overview = await GetOctoberAsync();

        Assert.Equal([_probe.Household, _probe.Mobility], overview.Expenses.Select(node => node.Category));
    }

    [Fact]
    public async Task CountsOnlyBookingsOfTheMonthButBothBookedAndPendingOnes()
    {
        _probe.AddBooking(_probe.Mobility, 60m, new DateOnly(2026, 9, 30));
        _probe.AddBooking(_probe.Mobility, 20m, new DateOnly(2026, 10, 1));
        _probe.AddBooking(_probe.Mobility, 5m, new DateOnly(2026, 10, 31), BookingStatus.Pending);
        _probe.AddBooking(_probe.Mobility, 80m, new DateOnly(2026, 11, 1));

        MonthOverviewData overview = await _probe.Workflow.GetMonthOverviewAsync(
            new DateOnly(2026, 10, 17),
            TestContext.Current.CancellationToken);

        Assert.Equal(October, overview.Month);
        Assert.Equal(25m, Assert.Single(overview.Expenses).TotalAmount);
    }

    [Fact]
    public async Task ReturnsTheTotalsAndTheBalance()
    {
        _probe.AddBooking(_probe.Salary, 3000m, new DateOnly(2026, 10, 1));
        _probe.AddBooking(_probe.Groceries, 40m, new DateOnly(2026, 10, 3));
        _probe.AddBooking(_probe.Mobility, 60m, new DateOnly(2026, 10, 9));

        MonthOverviewData overview = await GetOctoberAsync();

        Assert.Equal((3000m, 100m, 2900m), (overview.IncomeTotal, overview.ExpenseTotal, overview.Balance));
    }

    [Fact]
    public async Task ReturnsEmptyTreesAndZeroTotals_WhenTheMonthHasNoBookings()
    {
        MonthOverviewData overview = await GetOctoberAsync();

        Assert.Empty(overview.Incomes);
        Assert.Empty(overview.Expenses);
        Assert.Equal((0m, 0m, 0m), (overview.IncomeTotal, overview.ExpenseTotal, overview.Balance));
    }

    [Fact]
    public async Task LogsTheMonthAndTheNumberOfBookings()
    {
        _probe.AddBooking(_probe.Mobility, 60m, new DateOnly(2026, 10, 9));

        await GetOctoberAsync();

        LogEntry logEntry = Assert.Single(_probe.Logger.Entries);
        Assert.Equal(LogLevel.Debug, logEntry.LogLevel);
        Assert.Equal("Month overview created.", logEntry.Message);
        Assert.Equal("2026-10-01", logEntry.ContextData["Month"]);
        Assert.Equal("1", logEntry.ContextData["BookingCount"]);
    }

    private async Task<MonthOverviewData> GetOctoberAsync()
        => await _probe.Workflow.GetMonthOverviewAsync(October, TestContext.Current.CancellationToken);
}
