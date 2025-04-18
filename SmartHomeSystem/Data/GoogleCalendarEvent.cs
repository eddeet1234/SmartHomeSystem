namespace SmartHomeSystem.Data
{
    public class GoogleCalendarEvent
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Status { get; set; }  // confirmed, tentative, cancelled
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string HtmlLink { get; set; }
        public bool IsAllDay { get; set; }
        public string TimeZone { get; set; }
    }
}
