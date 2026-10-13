using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Event
{
    [Key]
    public int EventId { get; set; }

    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Province { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty; // Running, Walking, Cycling

    public decimal DistanceKm { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<EventRoute> Routes { get; set; } = new List<EventRoute>();
}