using System.Data;
using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using Npgsql;
using NpgsqlTypes;

namespace GwiOS.Core.Haushaltsbuch.Infrastructure;

/// <summary>
/// Stores Haushaltsbuch recurring items in the table <c>haushaltsbuch.recurring_items</c> of the GwiOS PostgreSQL
/// database, using plain SQL.
/// </summary>
internal sealed class RecurringItemPostgresRepository(
    NpgsqlDataSource dataSource,
    ILogger<RecurringItemPostgresRepository> logger) : IRecurringItemRepository
{
    private const string CreateTablesSql = """
        CREATE SCHEMA IF NOT EXISTS haushaltsbuch;

        CREATE TABLE IF NOT EXISTS haushaltsbuch.recurring_items
        (
            id          uuid          NOT NULL,
            name        text          NOT NULL,
            type        smallint      NOT NULL,
            category_id uuid          NOT NULL,
            amount      numeric(10,2) NOT NULL,
            interval    smallint      NOT NULL,
            due_day     integer       NOT NULL,
            start_month integer       NOT NULL,
            active      boolean       NOT NULL,
            CONSTRAINT pk_haushaltsbuch_recurring_items PRIMARY KEY (id),
            CONSTRAINT fk_haushaltsbuch_recurring_items_category FOREIGN KEY (category_id)
                REFERENCES haushaltsbuch.categories (id)
        );

        -- Added after the table was first deployed; ADD COLUMN IF NOT EXISTS extends existing tables as well.
        ALTER TABLE haushaltsbuch.recurring_items
            ADD COLUMN IF NOT EXISTS predecessor_id uuid NULL
                CONSTRAINT fk_haushaltsbuch_recurring_items_predecessor
                REFERENCES haushaltsbuch.recurring_items (id) ON DELETE SET NULL,
            ADD COLUMN IF NOT EXISTS valid_from date NULL,
            ADD COLUMN IF NOT EXISTS valid_until date NULL;
        """;

    private const string InsertSql = """
        INSERT INTO haushaltsbuch.recurring_items
            (id, name, type, category_id, amount, interval, due_day, start_month, active, predecessor_id, valid_from,
             valid_until)
        VALUES (@id, @name, @type, @category_id, @amount, @interval, @due_day, @start_month, @active, @predecessor_id,
                @valid_from, @valid_until)
        """;

    private const string UpdateSql = """
        UPDATE haushaltsbuch.recurring_items
        SET name = @name, type = @type, category_id = @category_id, amount = @amount, interval = @interval,
            due_day = @due_day, start_month = @start_month, active = @active, predecessor_id = @predecessor_id,
            valid_from = @valid_from, valid_until = @valid_until
        WHERE id = @id
        """;

    private const string SelectAllSql = """
        SELECT id, name, type, category_id, amount, interval, due_day, start_month, active, predecessor_id, valid_from,
               valid_until
        FROM haushaltsbuch.recurring_items
        ORDER BY type, name
        """;

    private const string DeleteSql = """
        DELETE FROM haushaltsbuch.recurring_items
        WHERE id = @id
        """;

    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly ILogger<RecurringItemPostgresRepository> _logger = logger;

    public async Task EnsureStorageCreatedAsync(CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(CreateTablesSql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertAsync(RecurringItem recurringItem, CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(InsertSql);
        AddRecurringItemParameters(command, recurringItem);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(RecurringItem recurringItem, CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(UpdateSql);
        AddRecurringItemParameters(command, recurringItem);
        int updatedRowCount = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updatedRowCount == 0)
        {
            _logger.LogWarning("Recurring item to update is not stored.", CreateRecurringItemIdData(recurringItem.Id));
            throw new RecurringItemNotFoundException(recurringItem.Id);
        }
    }

    public async Task ContinueAsync(
        RecurringItem endedPredecessor,
        RecurringItem successor,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using NpgsqlCommand updateCommand = new(UpdateSql, connection, transaction);
        AddRecurringItemParameters(updateCommand, endedPredecessor);
        if (await updateCommand.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            _logger.LogWarning("Recurring item to continue is not stored.", CreateRecurringItemIdData(endedPredecessor.Id));
            throw new RecurringItemNotFoundException(endedPredecessor.Id);
        }

        await using NpgsqlCommand insertCommand = new(InsertSql, connection, transaction);
        AddRecurringItemParameters(insertCommand, successor);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<List<RecurringItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(SelectAllSql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        List<RecurringItem> recurringItems = [];
        while (await reader.ReadAsync(cancellationToken))
        {
            recurringItems.Add(ReadRecurringItem(reader));
        }

        return recurringItems;
    }

    public async Task DeleteAsync(Guid recurringItemId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(DeleteSql);
        command.Parameters.AddWithValue("id", recurringItemId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddRecurringItemParameters(NpgsqlCommand command, RecurringItem recurringItem)
    {
        command.Parameters.AddWithValue("id", recurringItem.Id);
        command.Parameters.AddWithValue("name", recurringItem.Name);
        command.Parameters.AddWithValue("type", (short)recurringItem.Type);
        command.Parameters.AddWithValue("category_id", recurringItem.CategoryId);
        command.Parameters.AddWithValue("amount", recurringItem.Amount);
        command.Parameters.AddWithValue("interval", (short)recurringItem.Interval);
        command.Parameters.AddWithValue("due_day", recurringItem.DueDay);
        command.Parameters.AddWithValue("start_month", recurringItem.StartMonth);
        command.Parameters.AddWithValue("active", recurringItem.Active);
        command.Parameters.AddWithValue("predecessor_id", NpgsqlDbType.Uuid, (object?)recurringItem.PredecessorId ?? DBNull.Value);
        command.Parameters.AddWithValue("valid_from", NpgsqlDbType.Date, (object?)recurringItem.ValidFrom ?? DBNull.Value);
        command.Parameters.AddWithValue("valid_until", NpgsqlDbType.Date, (object?)recurringItem.ValidUntil ?? DBNull.Value);
    }

    private static RecurringItem ReadRecurringItem(NpgsqlDataReader reader)
        => new()
        {
            Id = reader.GetGuid("id"),
            Name = reader.GetString("name"),
            Type = (CategoryType)reader.GetFieldValue<short>("type"),
            CategoryId = reader.GetGuid("category_id"),
            Amount = reader.GetDecimal("amount"),
            Interval = (RecurringInterval)reader.GetFieldValue<short>("interval"),
            DueDay = reader.GetInt32("due_day"),
            StartMonth = reader.GetInt32("start_month"),
            Active = reader.GetBoolean("active"),
            PredecessorId = reader.IsDBNull("predecessor_id") ? null : reader.GetGuid("predecessor_id"),
            ValidFrom = reader.IsDBNull("valid_from") ? null : reader.GetFieldValue<DateOnly>("valid_from"),
            ValidUntil = reader.IsDBNull("valid_until") ? null : reader.GetFieldValue<DateOnly>("valid_until")
        };

    private static Dictionary<string, string> CreateRecurringItemIdData(Guid recurringItemId)
        => new() { ["RecurringItemId"] = recurringItemId.ToString() };
}
