namespace GwiOS.WebUI.BookingEntry.Contracts;

/// <summary>
/// Remembers what helps entering the next booking within the current user session, such as the date of the last new
/// booking, so that several receipts of one day can be entered in a row. Kept only for the lifetime of the session.
/// </summary>
public interface IBookingEntrySession
{
    /// <summary>
    /// The date of the booking created last in this session, or <c>null</c> if none was created yet.
    /// </summary>
    DateOnly? LastBookingDate { get; }

    /// <summary>
    /// Remembers the date of a booking that was just created as <see cref="LastBookingDate"/>.
    /// </summary>
    void RememberBookingDate(DateOnly date);
}
