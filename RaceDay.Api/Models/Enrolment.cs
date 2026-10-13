using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
    [Key]
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }
    public User? Participant { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string? BibNumber { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Confirmed"; // Confirmed or Cancelled

    public Result? Result { get; set; }
}