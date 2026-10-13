using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Result
{
    [Key]
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }

    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Finished"; // Finished, DNF, DSQ

    public int CapturedByUserId { get; set; }
    public User? CapturedBy { get; set; }

    public DateTime CapturedAt { get; set; } = DateTime.Now;
}