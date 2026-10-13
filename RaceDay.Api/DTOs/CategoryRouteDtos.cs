using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryDto
{
    /// <summary>For example "Under 20", "Senior", "10km" or "21km".</summary>
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }
}

public class RouteDto
{
    [Required, MaxLength(100)]
    public string RouteName { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    public int? ElevationGainM { get; set; }

    [Required, MaxLength(150)]
    public string StartPoint { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string EndPoint { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? GpxFileUrl { get; set; }
}