using System.Data;
using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using Npgsql;

namespace GwiOS.Core.Haushaltsbuch.Infrastructure;

/// <summary>
/// Stores Haushaltsbuch booking templates in the table <c>haushaltsbuch.booking_templates</c> of the GwiOS PostgreSQL
/// database, using plain SQL. A template is deleted together with its category by the foreign key.
/// </summary>
internal sealed class BookingTemplatePostgresRepository(
    NpgsqlDataSource dataSource,
    ILogger<BookingTemplatePostgresRepository> logger) : IBookingTemplateRepository
{
    private const string CreateTablesSql = """
        CREATE SCHEMA IF NOT EXISTS haushaltsbuch;

        CREATE TABLE IF NOT EXISTS haushaltsbuch.booking_templates
        (
            id          uuid    NOT NULL,
            description text    NOT NULL,
            category_id uuid    NOT NULL,
            position    integer NOT NULL,
            CONSTRAINT pk_haushaltsbuch_booking_templates PRIMARY KEY (id),
            CONSTRAINT fk_haushaltsbuch_booking_templates_category FOREIGN KEY (category_id)
                REFERENCES haushaltsbuch.categories (id) ON DELETE CASCADE
        );
        """;

    private const string InsertSql = """
        INSERT INTO haushaltsbuch.booking_templates (id, description, category_id, position)
        VALUES (@id, @description, @category_id, @position)
        """;

    private const string UpdateSql = """
        UPDATE haushaltsbuch.booking_templates
        SET description = @description, category_id = @category_id
        WHERE id = @id
        """;

    private const string UpdatePositionSql = """
        UPDATE haushaltsbuch.booking_templates
        SET position = @position
        WHERE id = @id
        """;

    private const string SelectAllSql = """
        SELECT id, description, category_id, position
        FROM haushaltsbuch.booking_templates
        ORDER BY position, id
        """;

    private const string DeleteSql = """
        DELETE FROM haushaltsbuch.booking_templates
        WHERE id = @id
        """;

    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly ILogger<BookingTemplatePostgresRepository> _logger = logger;

    public async Task EnsureStorageCreatedAsync(CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(CreateTablesSql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertAsync(BookingTemplate bookingTemplate, CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(InsertSql);
        command.Parameters.AddWithValue("id", bookingTemplate.Id);
        command.Parameters.AddWithValue("description", bookingTemplate.Description);
        command.Parameters.AddWithValue("category_id", bookingTemplate.CategoryId);
        command.Parameters.AddWithValue("position", bookingTemplate.Position);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(BookingTemplate bookingTemplate, CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(UpdateSql);
        command.Parameters.AddWithValue("id", bookingTemplate.Id);
        command.Parameters.AddWithValue("description", bookingTemplate.Description);
        command.Parameters.AddWithValue("category_id", bookingTemplate.CategoryId);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            ThrowNotFound(bookingTemplate.Id);
        }
    }

    public async Task UpdatePositionsAsync(
        IReadOnlyList<BookingTemplate> bookingTemplates,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (BookingTemplate bookingTemplate in bookingTemplates)
        {
            await using NpgsqlCommand command = new(UpdatePositionSql, connection, transaction);
            command.Parameters.AddWithValue("id", bookingTemplate.Id);
            command.Parameters.AddWithValue("position", bookingTemplate.Position);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                ThrowNotFound(bookingTemplate.Id);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<List<BookingTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(SelectAllSql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        List<BookingTemplate> bookingTemplates = [];
        while (await reader.ReadAsync(cancellationToken))
        {
            bookingTemplates.Add(ReadBookingTemplate(reader));
        }

        return bookingTemplates;
    }

    public async Task DeleteAsync(Guid bookingTemplateId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(DeleteSql);
        command.Parameters.AddWithValue("id", bookingTemplateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void ThrowNotFound(Guid bookingTemplateId)
    {
        _logger.LogWarning(
            "Booking template to update is not stored.",
            new Dictionary<string, string> { ["BookingTemplateId"] = bookingTemplateId.ToString() });
        throw new BookingTemplateNotFoundException(bookingTemplateId);
    }

    private static BookingTemplate ReadBookingTemplate(NpgsqlDataReader reader)
        => new()
        {
            Id = reader.GetGuid("id"),
            Description = reader.GetString("description"),
            CategoryId = reader.GetGuid("category_id"),
            Position = reader.GetInt32("position")
        };
}
