namespace GlobalActivityBot.Domain.Entities;

public class Guild
{
    public int Id { get; set; }
    public string DiscordId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserStat> UserStats { get; set; } = new List<UserStat>();
    public ICollection<ChannelStat> ChannelStats { get; set; } = new List<ChannelStat>();
}
