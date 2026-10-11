using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.WebUI.BookingEntry.Contracts;

namespace GwiOS.WebUI.BookingEntry;

internal sealed class BookingEntrySession(ILogger<BookingEntrySession> logger) : IBookingEntrySession
{
    private readonly ILogger<BookingEntrySession> _logger = logger;

    public DateOnly? LastBookingDate { get; private set; }

    public void RememberBookingDate(DateOnly date)
    {
        LastBookingDate = date;
        _logger.LogDebug("Booking date remembered for the session.");
    }
}
