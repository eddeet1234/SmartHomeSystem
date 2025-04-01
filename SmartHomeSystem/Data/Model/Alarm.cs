namespace SmartHomeSystem.Data.Model
{
    public class Alarm
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public string Label { get; set; }
        public bool RepeatDaily { get; set; }
    }
}
