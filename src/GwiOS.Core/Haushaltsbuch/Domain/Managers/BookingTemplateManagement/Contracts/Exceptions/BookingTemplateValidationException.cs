namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;

/// <summary>
/// Thrown when a booking template is rejected because it violates at least one validation rule.
/// </summary>
/// <param name="bookingTemplateId">The ID of the rejected booking template.</param>
/// <param name="validationErrors">One message per violated rule; never empty.</param>
public sealed class BookingTemplateValidationException(Guid bookingTemplateId, IReadOnlyList<string> validationErrors)
    : Exception($"Booking template '{bookingTemplateId}' is invalid: {string.Join(" ", validationErrors)}")
{
    /// <summary>
    /// The ID of the rejected booking template.
    /// </summary>
    public Guid BookingTemplateId { get; } = bookingTemplateId;

    /// <summary>
    /// One message per violated rule; never empty.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; } = validationErrors;
}
