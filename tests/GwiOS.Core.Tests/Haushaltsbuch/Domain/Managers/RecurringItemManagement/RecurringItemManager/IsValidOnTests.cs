using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.RecurringItemManagement.RecurringItemManager;

/// <summary>
/// Covers <c>RecurringItemManager.IsValidOn</c>.
/// </summary>
public sealed class IsValidOnTests
{
    private readonly RecurringItemManagerProbe _probe = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReturnsTheAnswerOfTheScheduler(bool isValid)
    {
        RecurringItem item = new()
        {
            Name = "Miete",
            Type = CategoryType.Expense,
            CategoryId = Guid.NewGuid(),
            Amount = 1150m,
            Interval = RecurringInterval.Monthly
        };
        _probe.RecurringItemScheduler.IsValid = isValid;

        Assert.Equal(isValid, _probe.RecurringItemManager.IsValidOn(item, new DateOnly(2026, 10, 1)));
    }
}
