namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;

/// <summary>
/// A favorite of the Haushaltsbuch: a description together with a category that is booked again and again (e.g.
/// "Edeka" to groceries). Picking it fills in the description, category and type of a new booking; amount and date
/// are entered each time, so a booking template deliberately stores neither.
/// </summary>
public class BookingTemplate
{
    /// <summary>
    /// The unique identifier of the booking template. Defaults to a new time-ordered UUID (version 7).
    /// </summary>
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>
    /// The description a booking made from this template gets, e.g. "Edeka". Must not be empty, must not exceed
    /// 200 characters, and must not be used by another template with the same <see cref="CategoryId"/> (ignoring
    /// case and surrounding white space).
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The ID of the category a booking made from this template is booked to. The type of the booking follows from
    /// this category. Deleting the category deletes the template with it.
    /// </summary>
    public required Guid CategoryId { get; set; }

    /// <summary>
    /// The place of the template in the list of all templates, which is ordered ascending by this value. Assigned
    /// when the template is created (behind all existing ones) and changed only by moving the template; the values
    /// need not be contiguous.
    /// </summary>
    public int Position { get; set; }
}
