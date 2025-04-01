using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeSystem.Data.Model
{
    public class LightSchedule
    {
        public int Id { get; set; }
        [Column(TypeName = "time")] // This tells EF to use TIME type in PostgreSQL
        public TimeSpan OnTime { get; set; } // Changed to TimeSpan for daily time
        [Column(TypeName = "time")]
        public TimeSpan? OffTime { get; set; } // Changed to TimeSpan for daily time
    }
}
