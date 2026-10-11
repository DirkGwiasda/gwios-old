using RecurringItemNotContinuableExceptionUnderTest =
    GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions.RecurringItemNotContinuableException;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions.RecurringItemNotContinuableException;

/// <summary>
/// Covers the constructor of <c>RecurringItemNotContinuableException</c>.
/// </summary>
public sealed class ConstructorTests
{
    private readonly Guid _recurringItemId = Guid.CreateVersion7();
    private readonly DateOnly _effectiveDate = new(2026, 11, 1);

    [Fact]
    public void KeepsTheRecurringItemIdAndTheEffectiveDate()
    {
        RecurringItemNotContinuableExceptionUnderTest exception = new(_recurringItemId, _effectiveDate);

        Assert.Equal(_recurringItemId, exception.RecurringItemId);
        Assert.Equal(_effectiveDate, exception.EffectiveDate);
    }

    [Fact]
    public void NamesTheRecurringItemIdAndTheEffectiveDateInTheMessage()
    {
        RecurringItemNotContinuableExceptionUnderTest exception = new(_recurringItemId, _effectiveDate);

        Assert.Equal(
            $"The recurring item with ID '{_recurringItemId}' cannot be continued from 2026-11-01: "
            + "it already has an end or does not start before that day.",
            exception.Message);
    }
}
