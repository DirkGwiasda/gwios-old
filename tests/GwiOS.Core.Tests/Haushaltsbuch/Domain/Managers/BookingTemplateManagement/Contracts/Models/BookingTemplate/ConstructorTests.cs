using BookingTemplateUnderTest =
    GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models.BookingTemplate;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models.BookingTemplate;

/// <summary>
/// Covers the construction of <c>BookingTemplate</c> and the defaults it assigns.
/// </summary>
public sealed class ConstructorTests
{
    private readonly Guid _categoryId = Guid.CreateVersion7();

    [Fact]
    public void AssignsAVersion7Id()
    {
        BookingTemplateUnderTest bookingTemplate = new() { Description = "Edeka", CategoryId = _categoryId };

        Assert.Equal(7, bookingTemplate.Id.Version);
    }

    [Fact]
    public void AssignsADifferentIdToEachBookingTemplate()
    {
        BookingTemplateUnderTest firstTemplate = new() { Description = "Edeka", CategoryId = _categoryId };
        BookingTemplateUnderTest secondTemplate = new() { Description = "Edeka", CategoryId = _categoryId };

        Assert.NotEqual(firstTemplate.Id, secondTemplate.Id);
    }

    [Fact]
    public void StartsAtPositionZero()
    {
        BookingTemplateUnderTest bookingTemplate = new() { Description = "Edeka", CategoryId = _categoryId };

        Assert.Equal(0, bookingTemplate.Position);
    }
}
