using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Tests.CrossCutting.Logging.Contracts;
using GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using AvailableBalanceWorkflowUnderTest =
    GwiOS.Core.Haushaltsbuch.Domain.Workflows.AvailableBalance.AvailableBalanceWorkflow;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Workflows.AvailableBalance.AvailableBalanceWorkflow;

/// <summary>
/// Runs an <c>AvailableBalanceWorkflow</c> against in-memory managers and a recording logger.
/// </summary>
internal sealed class AvailableBalanceWorkflowProbe
{
    public AvailableBalanceWorkflowProbe()
        => Workflow = new AvailableBalanceWorkflowUnderTest(RecurringItemManager, BookingManager, Logger);

    public RecurringItemManagerFake RecurringItemManager { get; } = new();

    public BookingManagerFake BookingManager { get; } = new();

    public LoggerFake<AvailableBalanceWorkflowUnderTest> Logger { get; } = new();

    public AvailableBalanceWorkflowUnderTest Workflow { get; }

    /// <summary>
    /// Stores and returns an active recurring item of the given type.
    /// </summary>
    public RecurringItem AddItem(
        CategoryType type,
        decimal amount,
        RecurringInterval interval = RecurringInterval.Monthly,
        int startMonth = 0)
    {
        RecurringItem item = new()
        {
            Name = $"Fixposten {amount}",
            Type = type,
            CategoryId = Guid.NewGuid(),
            Amount = amount,
            Interval = interval,
            StartMonth = startMonth
        };
        RecurringItemManager.Add(item);
        return item;
    }

    /// <summary>
    /// Stores a booking of the given type on the given day.
    /// </summary>
    public void AddBooking(
        CategoryType type,
        decimal amount,
        DateOnly date,
        BookingStatus status = BookingStatus.Booked,
        Guid? recurringItemId = null)
        => BookingManager.Add(new Booking
        {
            Date = date,
            Type = type,
            CategoryId = Guid.NewGuid(),
            Amount = amount,
            Status = status,
            RecurringItemId = recurringItemId
        });
}
