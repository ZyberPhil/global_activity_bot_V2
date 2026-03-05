namespace GlobalActivityBot.Domain.Entities;

public class UserStat
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int GuildId { get; set; }
    public int Xp { get; set; }
    public int Level { get; set; }
    public int MessageCount { get; set; }
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Guild Guild { get; set; } = null!;
}
