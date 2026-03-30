using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodSafetyInspection.Domain
{
    public class FollowUp
    {
        public int Id { get; set; }
        public int InspectionId { get; set; }

        [ValidateNever]
        public Inspection Inspection { get; set; } = null!;

        public DateTime DueDate { get; set; }
        public FollowUpStatus Status { get; set; }
        public DateTime? ClosedDate { get; set; }
    }

    public enum FollowUpStatus { Open, Closed }
}