using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class EventRoute
{
    [Key]
    public int RouteId { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    [Required, MaxLength(100)]
    public string RouteName { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }
    public int? ElevationGainM { get; set; }

    [Required, MaxLength(150)]
    public string StartPoint { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string EndPoint { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? GpxFileUrl { get; set; }
}