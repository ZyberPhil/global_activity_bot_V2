namespace GlobalActivityBot.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string DiscordId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int GlobalXp { get; set; }
    public int GlobalLevel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserStat> Stats { get; set; } = new List<UserStat>();
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
