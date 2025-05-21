using System;

namespace SmartHomeSystem.Data.Model
{
    public class Temperature
    {
        public int Id { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 