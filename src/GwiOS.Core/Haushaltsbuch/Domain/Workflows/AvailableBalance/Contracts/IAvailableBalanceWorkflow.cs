namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.AvailableBalance.Contracts;

/// <summary>
/// Calculates the balance available in a month of the Haushaltsbuch from its recurring items and its other bookings.
/// </summary>
public interface IAvailableBalanceWorkflow
{
    /// <summary>
    /// Returns the available balance of the month of <paramref name="month"/> in euros: the recurring income falling
    /// due in that month plus the booked other income, minus the recurring expenses falling due in that month and the
    /// booked other expenses. Recurring items count only while active, once per due date and whether booked or not;
    /// other bookings are those without a recurring item that have the status booked. Negative if expenses prevail.
    /// </summary>
    Task<decimal> GetAvailableBalanceAsync(DateOnly month, CancellationToken cancellationToken = default);
}
