namespace sentinel_api.Models
{
    public class EmailConfirmToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; } = DateTime.UtcNow.AddHours(1);
    }
}
