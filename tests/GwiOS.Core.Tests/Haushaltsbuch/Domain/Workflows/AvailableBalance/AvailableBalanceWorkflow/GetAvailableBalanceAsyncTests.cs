using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Workflows.AvailableBalance.AvailableBalanceWorkflow;

/// <summary>
/// Covers <c>AvailableBalanceWorkflow.GetAvailableBalanceAsync</c>.
/// </summary>
public sealed class GetAvailableBalanceAsyncTests
{
    private static readonly DateOnly October = new(2026, 10, 1);

    private readonly AvailableBalanceWorkflowProbe _probe = new();

    [Fact]
    public async Task ReturnsZero_WhenThereAreNeitherRecurringItemsNorBookings()
    {
        Assert.Equal(0m, await GetAvailableBalanceAsync(October));
    }

    [Fact]
    public async Task AddsRecurringIncomeAndBookedOtherIncomeAndSubtractsRecurringAndBookedOtherExpenses()
    {
        _probe.AddItem(CategoryType.Income, 3000m);
        _probe.AddItem(CategoryType.Expense, 1150m);
        _probe.AddBooking(CategoryType.Income, 200m, new DateOnly(2026, 10, 12));
        _probe.AddBooking(CategoryType.Expense, 85.5m, new DateOnly(2026, 10, 20));

        Assert.Equal(1964.5m, await GetAvailableBalanceAsync(October));
    }

    [Fact]
    public async Task CountsRecurringItemsEvenWhenTheyAreNotBooked()
    {
        _probe.AddItem(CategoryType.Expense, 1150m);

        Assert.Equal(-1150m, await GetAvailableBalanceAsync(October));
    }

    [Fact]
    public async Task CountsABookingGeneratedFromARecurringItemOnlyOnceThroughTheRecurringItem()
    {
        RecurringItem rent = _probe.AddItem(CategoryType.Expense, 1150m);
        _probe.AddBooking(CategoryType.Expense, 1150m, new DateOnly(2026, 10, 1), recurringItemId: rent.Id);

        Assert.Equal(-1150m, await GetAvailableBalanceAsync(October));
    }

    [Fact]
    public async Task LeavesOutPendingOtherBookings()
    {
        _probe.AddBooking(CategoryType.Income, 200m, new DateOnly(2026, 10, 12), BookingStatus.Pending);
        _probe.AddBooking(CategoryType.Expense, 85.5m, new DateOnly(2026, 10, 20), BookingStatus.Pending);

        Assert.Equal(0m, await GetAvailableBalanceAsync(October));
    }

    [Fact]
    public async Task LeavesOutBookingsOfOtherMonths()
    {
        _probe.AddBooking(CategoryType.Expense, 50m, new DateOnly(2026, 9, 30));
        _probe.AddBooking(CategoryType.Expense, 60m, new DateOnly(2026, 11, 1));

        Assert.Equal(0m, await GetAvailableBalanceAsync(October));
    }

    [Fact]
    public async Task LeavesOutInactiveRecurringItems()
    {
        RecurringItem item = _probe.AddItem(CategoryType.Expense, 39.9m);
        item.Active = false;

        Assert.Equal(0m, await GetAvailableBalanceAsync(October));
    }

    [Fact]
    public async Task CountsAWeeklyRecurringItemOncePerDueDate()
    {
        _probe.AddItem(CategoryType.Expense, 100m, RecurringInterval.Weekly);

        Assert.Equal(-400m, await GetAvailableBalanceAsync(October));
    }

    [Fact]
    public async Task CountsAContinuedRecurringItemWithTheAmountInEffectOnEachDueDate()
    {
        RecurringItem before = _probe.AddItem(CategoryType.Expense, 100m, RecurringInterval.Weekly);
        before.ValidUntil = new DateOnly(2026, 10, 14);
        RecurringItem after = _probe.AddItem(CategoryType.Expense, 120m, RecurringInterval.Weekly);
        after.PredecessorId = before.Id;
        after.ValidFrom = new DateOnly(2026, 10, 15);

        Assert.Equal(-440m, await GetAvailableBalanceAsync(October));
    }

    [Fact]
    public async Task LeavesOutRecurringItemsNotDueInTheMonth()
    {
        // StartMonth 0 is January (zero-based): due in January, April, July and October, not in November.
        _probe.AddItem(CategoryType.Expense, 312m, RecurringInterval.Quarterly, startMonth: 0);

        Assert.Equal(0m, await GetAvailableBalanceAsync(new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public async Task UsesTheWholeMonth_WhenADayInTheMiddleOfTheMonthIsGiven()
    {
        _probe.AddBooking(CategoryType.Income, 20m, new DateOnly(2026, 10, 1));
        _probe.AddBooking(CategoryType.Income, 30m, new DateOnly(2026, 10, 31));

        Assert.Equal(50m, await GetAvailableBalanceAsync(new DateOnly(2026, 10, 17)));
    }

    [Fact]
    public async Task LogsTheCalculationAsDebug()
    {
        await GetAvailableBalanceAsync(new DateOnly(2026, 10, 17));

        LogEntry logEntry = Assert.Single(_probe.Logger.Entries);
        Assert.Equal(LogLevel.Debug, logEntry.LogLevel);
        Assert.Equal("2026-10-01", logEntry.ContextData["Month"]);
    }

    private Task<decimal> GetAvailableBalanceAsync(DateOnly month)
        => _probe.Workflow.GetAvailableBalanceAsync(month, TestContext.Current.CancellationToken);
}
