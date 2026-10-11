using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;

namespace GwiOS.WebUI.Tests.TestInfrastructure;

/// <summary>
/// Builds month overviews for the tests of the dialog and the PDF.
/// </summary>
public static class TestMonthOverviews
{
    /// <summary>
    /// Returns an overview of the given month without any bookings.
    /// </summary>
    public static MonthOverviewData CreateEmpty(DateOnly month)
        => new() { Month = month, Incomes = [], Expenses = [], IncomeTotal = 0, ExpenseTotal = 0, Balance = 0 };

    /// <summary>
    /// Returns an overview of October 2026: income "Gehalt" 3.000 €; expenses "Haushalt" (10 € directly) with
    /// "Lebensmittel" (47,50 €) and, below it, "Bio" (2,50 €), and "Mobilität" (60 €). Balance 2.880 €.
    /// </summary>
    public static MonthOverviewData CreateOctober()
    {
        Category household = new() { Name = "Haushalt", Type = CategoryType.Expense, Color = "#ffd056" };
        Category groceries = new() { Name = "Lebensmittel", Type = CategoryType.Expense, ParentId = household.Id };
        Category organic = new() { Name = "Bio", Type = CategoryType.Expense, ParentId = groceries.Id };
        Category mobility = new() { Name = "Mobilität", Type = CategoryType.Expense, Color = "#3399ff" };
        Category salary = new() { Name = "Gehalt", Type = CategoryType.Income, Color = "#00ffa3" };
        MonthOverviewNode organicNode = CreateNode(organic, 2.5m, 2.5m, 1, []);
        MonthOverviewNode groceriesNode = CreateNode(groceries, 47.5m, 50m, 2, [organicNode]);
        return new MonthOverviewData
        {
            Month = new DateOnly(2026, 10, 1),
            Incomes = [CreateNode(salary, 3000m, 3000m, 1, [])],
            Expenses = [CreateNode(household, 10m, 60m, 1, [groceriesNode]), CreateNode(mobility, 60m, 60m, 1, [])],
            IncomeTotal = 3000m,
            ExpenseTotal = 120m,
            Balance = 2880m
        };
    }

    /// <summary>
    /// Returns a node of the given category with the given amounts and sub-categories.
    /// </summary>
    public static MonthOverviewNode CreateNode(
        Category category, decimal ownAmount, decimal totalAmount, int bookingCount, IReadOnlyList<MonthOverviewNode> children)
        => new()
        {
            Category = category,
            OwnAmount = ownAmount,
            TotalAmount = totalAmount,
            BookingCount = bookingCount,
            Children = children
        };
}
