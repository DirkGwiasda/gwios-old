using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using RecurringItemSchedulerUnderTest = GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.RecurringItemScheduler;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.RecurringItemManagement.RecurringItemScheduler;

/// <summary>
/// Covers <c>RecurringItemScheduler.IsValidOn</c>.
/// </summary>
public sealed class IsValidOnTests
{
    private readonly RecurringItemSchedulerUnderTest _scheduler = new();

    [Fact]
    public void ReturnsTrue_WhenTheValidityIsUnbounded()
    {
        Assert.True(_scheduler.IsValidOn(CreateItem(validFrom: null, validUntil: null), new DateOnly(1990, 1, 1)));
    }

    [Theory]
    [InlineData(2026, 10, 31, false)]
    [InlineData(2026, 11, 1, true)]
    [InlineData(2026, 11, 30, true)]
    [InlineData(2026, 12, 1, false)]
    public void ReturnsWhetherTheDayLiesWithinTheValidityBothEndsIncluded(int year, int month, int day, bool isValid)
    {
        RecurringItem item = CreateItem(validFrom: new DateOnly(2026, 11, 1), validUntil: new DateOnly(2026, 11, 30));

        Assert.Equal(isValid, _scheduler.IsValidOn(item, new DateOnly(year, month, day)));
    }

    [Fact]
    public void IgnoresWhetherTheItemIsActive()
    {
        RecurringItem item = CreateItem(validFrom: null, validUntil: null);
        item.Active = false;

        Assert.True(_scheduler.IsValidOn(item, new DateOnly(2026, 10, 1)));
    }

    private static RecurringItem CreateItem(DateOnly? validFrom, DateOnly? validUntil)
        => new()
        {
            Name = "Posten",
            Type = CategoryType.Expense,
            CategoryId = Guid.NewGuid(),
            Amount = 10m,
            Interval = RecurringInterval.Monthly,
            ValidFrom = validFrom,
            ValidUntil = validUntil
        };
}
