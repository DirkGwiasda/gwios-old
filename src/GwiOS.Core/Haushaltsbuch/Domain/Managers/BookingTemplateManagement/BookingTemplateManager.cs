using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement;

internal sealed class BookingTemplateManager(
    IBookingTemplateRepository bookingTemplateRepository,
    IBookingTemplateValidator bookingTemplateValidator,
    ILogger<BookingTemplateManager> logger) : IBookingTemplateManager
{
    private readonly IBookingTemplateRepository _bookingTemplateRepository = bookingTemplateRepository;
    private readonly IBookingTemplateValidator _bookingTemplateValidator = bookingTemplateValidator;
    private readonly ILogger<BookingTemplateManager> _logger = logger;

    public async Task<BookingTemplate> CreateBookingTemplateAsync(
        BookingTemplate bookingTemplate,
        CancellationToken cancellationToken = default)
    {
        List<BookingTemplate> existingTemplates = await _bookingTemplateRepository.GetAllAsync(cancellationToken);
        ThrowIfInvalid(bookingTemplate, existingTemplates);
        bookingTemplate.Position = GetPositionBehind(existingTemplates);
        await _bookingTemplateRepository.InsertAsync(bookingTemplate, cancellationToken);
        _logger.LogInformation("Booking template created.", CreateBookingTemplateIdData(bookingTemplate.Id));
        return bookingTemplate;
    }

    public async Task UpdateBookingTemplateAsync(
        BookingTemplate bookingTemplate,
        CancellationToken cancellationToken = default)
    {
        List<BookingTemplate> existingTemplates = await _bookingTemplateRepository.GetAllAsync(cancellationToken);
        ThrowIfInvalid(bookingTemplate, existingTemplates);
        await _bookingTemplateRepository.UpdateAsync(bookingTemplate, cancellationToken);
        _logger.LogInformation("Booking template updated.", CreateBookingTemplateIdData(bookingTemplate.Id));
    }

    public async Task MoveBookingTemplateAsync(
        Guid bookingTemplateId,
        BookingTemplateMoveDirection direction,
        CancellationToken cancellationToken = default)
    {
        List<BookingTemplate> templates = await _bookingTemplateRepository.GetAllAsync(cancellationToken);
        int index = templates.FindIndex(template => template.Id == bookingTemplateId);
        if (index < 0)
        {
            throw new BookingTemplateNotFoundException(bookingTemplateId);
        }

        int neighborIndex = direction == BookingTemplateMoveDirection.Up ? index - 1 : index + 1;
        if ((neighborIndex < 0) || (neighborIndex >= templates.Count))
        {
            return;
        }

        (templates[index], templates[neighborIndex]) = (templates[neighborIndex], templates[index]);
        await StorePositionsInListOrderAsync(templates, cancellationToken);
        _logger.LogInformation("Booking template moved.", CreateBookingTemplateIdData(bookingTemplateId));
    }

    public async Task<List<BookingTemplate>> GetAllBookingTemplatesAsync(CancellationToken cancellationToken = default)
        => await _bookingTemplateRepository.GetAllAsync(cancellationToken);

    public async Task DeleteBookingTemplateAsync(Guid bookingTemplateId, CancellationToken cancellationToken = default)
    {
        await _bookingTemplateRepository.DeleteAsync(bookingTemplateId, cancellationToken);
        _logger.LogInformation("Booking template deleted.", CreateBookingTemplateIdData(bookingTemplateId));
    }

    private void ThrowIfInvalid(BookingTemplate bookingTemplate, List<BookingTemplate> existingTemplates)
    {
        if (!_bookingTemplateValidator.IsValid(bookingTemplate, existingTemplates, out List<string> validationErrors))
        {
            throw new BookingTemplateValidationException(bookingTemplate.Id, validationErrors);
        }
    }

    private static int GetPositionBehind(List<BookingTemplate> existingTemplates)
        => existingTemplates.Count == 0 ? 0 : existingTemplates.Max(template => template.Position) + 1;

    // Numbering all templates afresh, rather than swapping two positions, also separates templates that ended up with
    // the same position (e.g. created at the same time), which a swap would leave in place.
    private async Task StorePositionsInListOrderAsync(
        List<BookingTemplate> templates,
        CancellationToken cancellationToken)
    {
        for (int index = 0; index < templates.Count; index++)
        {
            templates[index].Position = index;
        }

        await _bookingTemplateRepository.UpdatePositionsAsync(templates, cancellationToken);
    }

    private static Dictionary<string, string> CreateBookingTemplateIdData(Guid bookingTemplateId)
        => new() { ["BookingTemplateId"] = bookingTemplateId.ToString() };
}
