using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodSafetyInspection.Domain
{
    public class Inspection
    {
        public int Id { get; set; }

        public int PremisesId { get; set; }

        [ValidateNever]
        public Premises Premises { get; set; } = null!;

        [Required]
        public DateTime InspectionDate { get; set; }

        [Range(0, 100, ErrorMessage = "Score must be between 0 and 100.")]
        public int Score { get; set; }

        public InspectionOutcome Outcome { get; set; }

        public string? Notes { get; set; }

        public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    }

    public enum InspectionOutcome { Pass, Fail }
}