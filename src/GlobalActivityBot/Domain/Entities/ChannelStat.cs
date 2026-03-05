namespace GlobalActivityBot.Domain.Entities;

public class ChannelStat
{
    public int Id { get; set; }
    public int GuildId { get; set; }
    public string DiscordChannelId { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public Guild Guild { get; set; } = null!;
}
