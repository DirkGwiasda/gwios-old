using BookingTemplateNotFoundExceptionUnderTest =
    GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions.BookingTemplateNotFoundException;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions.BookingTemplateNotFoundException;

/// <summary>
/// Covers the constructor of <c>BookingTemplateNotFoundException</c>.
/// </summary>
public sealed class ConstructorTests
{
    private readonly Guid _bookingTemplateId = Guid.CreateVersion7();

    [Fact]
    public void KeepsTheBookingTemplateId()
    {
        BookingTemplateNotFoundExceptionUnderTest exception = new(_bookingTemplateId);

        Assert.Equal(_bookingTemplateId, exception.BookingTemplateId);
    }

    [Fact]
    public void NamesTheBookingTemplateIdInTheMessage()
    {
        BookingTemplateNotFoundExceptionUnderTest exception = new(_bookingTemplateId);

        Assert.Equal($"No booking template with ID '{_bookingTemplateId}' is stored.", exception.Message);
    }
}
