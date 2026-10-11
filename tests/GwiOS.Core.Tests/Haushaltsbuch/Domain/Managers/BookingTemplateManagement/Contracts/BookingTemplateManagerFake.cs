using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;

/// <summary>
/// Stands in for <see cref="IBookingTemplateManager"/> for the workflow tests. It keeps booking templates in memory
/// in the order they were added, but neither validates nor reorders them.
/// </summary>
public sealed class BookingTemplateManagerFake : IBookingTemplateManager
{
    private readonly List<BookingTemplate> _bookingTemplates = [];

    /// <summary>
    /// Stores a template directly, as if it had been created before the test.
    /// </summary>
    public void Add(BookingTemplate bookingTemplate)
        => _bookingTemplates.Add(bookingTemplate);

    public Task<BookingTemplate> CreateBookingTemplateAsync(
        BookingTemplate bookingTemplate,
        CancellationToken cancellationToken = default)
    {
        _bookingTemplates.Add(bookingTemplate);
        return Task.FromResult(bookingTemplate);
    }

    public Task UpdateBookingTemplateAsync(
        BookingTemplate bookingTemplate,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task MoveBookingTemplateAsync(
        Guid bookingTemplateId,
        BookingTemplateMoveDirection direction,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<List<BookingTemplate>> GetAllBookingTemplatesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_bookingTemplates.ToList());

    public Task DeleteBookingTemplateAsync(Guid bookingTemplateId, CancellationToken cancellationToken = default)
    {
        _bookingTemplates.RemoveAll(bookingTemplate => bookingTemplate.Id == bookingTemplateId);
        return Task.CompletedTask;
    }
}
