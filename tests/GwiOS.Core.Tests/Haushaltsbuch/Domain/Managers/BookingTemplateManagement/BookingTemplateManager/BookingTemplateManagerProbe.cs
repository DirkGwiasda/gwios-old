using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Tests.CrossCutting.Logging.Contracts;
using GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using BookingTemplateManagerUnderTest =
    GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.BookingTemplateManager;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.BookingTemplateManager;

/// <summary>
/// Runs a <c>BookingTemplateManager</c> against an in-memory repository, a configurable validator and a recording
/// logger, and gives the tests access to all three.
/// </summary>
internal sealed class BookingTemplateManagerProbe
{
    public BookingTemplateManagerProbe()
    {
        BookingTemplateManager =
            new BookingTemplateManagerUnderTest(BookingTemplateRepository, BookingTemplateValidator, Logger);
    }

    /// <summary>
    /// The in-memory repository the manager stores its booking templates in.
    /// </summary>
    public BookingTemplateRepositoryFake BookingTemplateRepository { get; } = new();

    /// <summary>
    /// The validator the manager checks booking templates with; every template is valid until the test sets errors.
    /// </summary>
    public BookingTemplateValidatorFake BookingTemplateValidator { get; } = new();

    /// <summary>
    /// The logger the manager writes to.
    /// </summary>
    public LoggerFake<BookingTemplateManagerUnderTest> Logger { get; } = new();

    /// <summary>
    /// The manager under test.
    /// </summary>
    public BookingTemplateManagerUnderTest BookingTemplateManager { get; }

    /// <summary>
    /// Stores a template with the given description and position directly in the repository and returns it.
    /// </summary>
    public async Task<BookingTemplate> StoreAsync(string description, int position)
    {
        BookingTemplate bookingTemplate = new()
        {
            Description = description, CategoryId = Guid.CreateVersion7(), Position = position
        };
        await BookingTemplateRepository.InsertAsync(bookingTemplate);
        return bookingTemplate;
    }

    /// <summary>
    /// Returns the descriptions of all stored templates, in the order the repository returns them.
    /// </summary>
    public async Task<List<string>> GetStoredDescriptionsAsync()
        => [.. (await BookingTemplateRepository.GetAllAsync()).Select(bookingTemplate => bookingTemplate.Description)];
}
