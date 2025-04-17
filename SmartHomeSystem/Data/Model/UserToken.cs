namespace SmartHomeSystem.Data.Model
{
    public class UserToken
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }

}
