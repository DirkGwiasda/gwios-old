using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.WebUI.Tests.TestInfrastructure;
using BookingEntrySessionUnderTest = GwiOS.WebUI.BookingEntry.BookingEntrySession;

namespace GwiOS.WebUI.Tests.BookingEntry.BookingEntrySession;

/// <summary>
/// Covers <c>BookingEntrySession.RememberBookingDate</c> and the date it keeps.
/// </summary>
public sealed class RememberBookingDateTests
{
    private readonly LoggerFake<BookingEntrySessionUnderTest> _logger = new();
    private readonly BookingEntrySessionUnderTest _session;

    public RememberBookingDateTests()
    {
        _session = new BookingEntrySessionUnderTest(_logger);
    }

    [Fact]
    public void KeepsNoDate_BeforeABookingWasCreated()
    {
        Assert.Null(_session.LastBookingDate);
    }

    [Fact]
    public void KeepsTheDateRememberedLast()
    {
        _session.RememberBookingDate(new DateOnly(2026, 10, 3));
        _session.RememberBookingDate(new DateOnly(2026, 10, 4));

        Assert.Equal(new DateOnly(2026, 10, 4), _session.LastBookingDate);
    }

    [Fact]
    public void LogsWithoutTheDate()
    {
        _session.RememberBookingDate(new DateOnly(2026, 10, 3));

        LogEntry logEntry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Debug, logEntry.LogLevel);
        Assert.Equal("Booking date remembered for the session.", logEntry.Message);
        Assert.Empty(logEntry.ContextData);
    }
}
