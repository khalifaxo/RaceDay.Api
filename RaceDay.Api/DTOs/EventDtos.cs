using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventDto
{
    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Province { get; set; } = string.Empty;

    /// <summary>Must be "Running", "Walking" or "Cycling".</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }
}