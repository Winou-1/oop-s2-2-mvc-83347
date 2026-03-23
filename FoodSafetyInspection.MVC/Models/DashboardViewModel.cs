using FoodSafetyInspection.Domain;

namespace FoodSafetyInspection.MVC.Models
{
    public class DashboardViewModel
    {
        public int InspectionsThisMonth { get; set; }
        public int FailedThisMonth { get; set; }
        public int OverdueFollowUps { get; set; }
        public string? FilterTown { get; set; }
        public RiskRating? FilterRiskRating { get; set; }
        public List<string> Towns { get; set; } = new();
    }
}
