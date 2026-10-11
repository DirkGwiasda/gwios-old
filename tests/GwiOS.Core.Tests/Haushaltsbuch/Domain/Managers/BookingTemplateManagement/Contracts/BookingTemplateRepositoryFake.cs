using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;

/// <summary>
/// Stands in for <see cref="IBookingTemplateRepository"/> by keeping booking templates in memory. It orders, updates
/// and deletes like the real repository, and records every call to <see cref="UpdatePositionsAsync"/>.
/// </summary>
public sealed class BookingTemplateRepositoryFake : IBookingTemplateRepository
{
    private readonly List<BookingTemplate> _bookingTemplates = [];

    /// <summary>
    /// The templates passed to each call of <see cref="UpdatePositionsAsync"/>, in call order.
    /// </summary>
    public List<IReadOnlyList<BookingTemplate>> PositionUpdates { get; } = [];

    public Task EnsureStorageCreatedAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InsertAsync(BookingTemplate bookingTemplate, CancellationToken cancellationToken = default)
    {
        _bookingTemplates.Add(bookingTemplate);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BookingTemplate bookingTemplate, CancellationToken cancellationToken = default)
    {
        BookingTemplate stored = FindStored(bookingTemplate.Id);
        stored.Description = bookingTemplate.Description;
        stored.CategoryId = bookingTemplate.CategoryId;
        return Task.CompletedTask;
    }

    public Task UpdatePositionsAsync(
        IReadOnlyList<BookingTemplate> bookingTemplates,
        CancellationToken cancellationToken = default)
    {
        PositionUpdates.Add(bookingTemplates);
        foreach (BookingTemplate bookingTemplate in bookingTemplates)
        {
            FindStored(bookingTemplate.Id).Position = bookingTemplate.Position;
        }

        return Task.CompletedTask;
    }

    public Task<List<BookingTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_bookingTemplates
            .OrderBy(bookingTemplate => bookingTemplate.Position)
            .ThenBy(bookingTemplate => bookingTemplate.Id)
            .ToList());

    public Task DeleteAsync(Guid bookingTemplateId, CancellationToken cancellationToken = default)
    {
        _bookingTemplates.RemoveAll(bookingTemplate => bookingTemplate.Id == bookingTemplateId);
        return Task.CompletedTask;
    }

    private BookingTemplate FindStored(Guid bookingTemplateId)
        => _bookingTemplates.SingleOrDefault(stored => stored.Id == bookingTemplateId)
            ?? throw new BookingTemplateNotFoundException(bookingTemplateId);
}
