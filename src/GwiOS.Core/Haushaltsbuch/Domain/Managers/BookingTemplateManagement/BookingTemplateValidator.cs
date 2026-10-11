using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement;

internal sealed class BookingTemplateValidator(ILogger<BookingTemplateValidator> logger) : IBookingTemplateValidator
{
    // The same limit as the description field of a booking, which a template fills in.
    private const int MaxDescriptionLength = 200;

    private readonly ILogger<BookingTemplateValidator> _logger = logger;

    public bool IsValid(
        BookingTemplate bookingTemplate,
        IReadOnlyList<BookingTemplate> existingBookingTemplates,
        out List<string> validationErrors)
    {
        validationErrors = CollectValidationErrors(bookingTemplate, existingBookingTemplates);
        if (validationErrors.Count == 0)
        {
            return true;
        }

        LogValidationErrors(bookingTemplate, validationErrors);
        return false;
    }

    private static List<string> CollectValidationErrors(
        BookingTemplate bookingTemplate,
        IReadOnlyList<BookingTemplate> existingBookingTemplates)
    {
        List<string> validationErrors = [];

        if (string.IsNullOrWhiteSpace(bookingTemplate.Description))
        {
            validationErrors.Add("The description must not be empty.");
        }
        else if (bookingTemplate.Description.Length > MaxDescriptionLength)
        {
            validationErrors.Add($"The description must not exceed {MaxDescriptionLength} characters.");
        }

        if (bookingTemplate.CategoryId == Guid.Empty)
        {
            validationErrors.Add("The category is required.");
        }

        if (IsDuplicate(bookingTemplate, existingBookingTemplates))
        {
            validationErrors.Add("Another booking template already uses this description with this category.");
        }

        return validationErrors;
    }

    private static bool IsDuplicate(
        BookingTemplate bookingTemplate,
        IReadOnlyList<BookingTemplate> existingBookingTemplates)
        => !string.IsNullOrWhiteSpace(bookingTemplate.Description)
            && existingBookingTemplates.Any(existing =>
                (existing.Id != bookingTemplate.Id)
                && (existing.CategoryId == bookingTemplate.CategoryId)
                && string.Equals(
                    existing.Description.Trim(),
                    bookingTemplate.Description.Trim(),
                    StringComparison.OrdinalIgnoreCase));

    private void LogValidationErrors(BookingTemplate bookingTemplate, List<string> validationErrors)
        => _logger.LogWarning(
            "Booking template is invalid.",
            new Dictionary<string, string>
            {
                ["BookingTemplateId"] = bookingTemplate.Id.ToString(),
                ["ValidationErrors"] = string.Join(" ", validationErrors)
            });
}
