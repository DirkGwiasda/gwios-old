using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;

/// <summary>
/// Stands in for <see cref="IBookingTemplateValidator"/>. It considers every booking template valid until a test sets
/// <see cref="ValidationErrors"/>, and records every template it validates together with the existing templates it
/// was given.
/// </summary>
public sealed class BookingTemplateValidatorFake : IBookingTemplateValidator
{
    private readonly List<BookingTemplate> _validatedBookingTemplates = [];

    /// <summary>
    /// The validation errors reported for every template. Empty by default, which makes every template valid.
    /// </summary>
    public List<string> ValidationErrors { get; set; } = [];

    /// <summary>
    /// All templates validated so far, in the order of validation.
    /// </summary>
    public IReadOnlyList<BookingTemplate> ValidatedBookingTemplates
        => _validatedBookingTemplates;

    /// <summary>
    /// The <c>existingBookingTemplates</c> argument of the most recent call to <see cref="IsValid"/>, or <c>null</c>
    /// if it was not called yet.
    /// </summary>
    public IReadOnlyList<BookingTemplate>? LastExistingBookingTemplates { get; private set; }

    public bool IsValid(
        BookingTemplate bookingTemplate,
        IReadOnlyList<BookingTemplate> existingBookingTemplates,
        out List<string> validationErrors)
    {
        _validatedBookingTemplates.Add(bookingTemplate);
        LastExistingBookingTemplates = existingBookingTemplates;
        validationErrors = [.. ValidationErrors];
        return validationErrors.Count == 0;
    }
}
