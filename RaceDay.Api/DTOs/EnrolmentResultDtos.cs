using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EnrolDto
{
    /// <summary>The category the participant wants to enter.</summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class CreateResultDto
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    /// <summary>Finish time as hh:mm:ss, for example 03:45:10.</summary>
    public string? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }

    /// <summary>Must be "Finished", "DNF" or "DSQ".</summary>
    [Required]
    public string Status { get; set; } = "Finished";
}

public class UpdateResultDto
{
    /// <summary>Finish time as hh:mm:ss, for example 03:45:10.</summary>
    public string? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }

    /// <summary>Must be "Finished", "DNF" or "DSQ".</summary>
    [Required]
    public string Status { get; set; } = "Finished";
}