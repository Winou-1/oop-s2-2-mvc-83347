using System;
using System.Collections.Generic;
using System.Text;

namespace FoodSafetyInspection.Domain
{
    public class Premises
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Town { get; set; } = string.Empty;
        public RiskRating RiskRating { get; set; }
        public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    }
    public enum RiskRating { Low, Medium, High }

}
