using BookingTemplateValidationExceptionUnderTest =
    GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions.BookingTemplateValidationException;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions.BookingTemplateValidationException;

/// <summary>
/// Covers the constructor of <c>BookingTemplateValidationException</c>.
/// </summary>
public sealed class ConstructorTests
{
    private readonly Guid _bookingTemplateId = Guid.CreateVersion7();

    [Fact]
    public void KeepsTheBookingTemplateIdAndTheValidationErrors()
    {
        List<string> validationErrors = ["First error.", "Second error."];

        BookingTemplateValidationExceptionUnderTest exception = new(_bookingTemplateId, validationErrors);

        Assert.Equal(_bookingTemplateId, exception.BookingTemplateId);
        Assert.Same(validationErrors, exception.ValidationErrors);
    }

    [Fact]
    public void NamesTheBookingTemplateIdAndAllValidationErrorsInTheMessage()
    {
        BookingTemplateValidationExceptionUnderTest exception =
            new(_bookingTemplateId, ["First error.", "Second error."]);

        Assert.Equal(
            $"Booking template '{_bookingTemplateId}' is invalid: First error. Second error.",
            exception.Message);
    }
}
