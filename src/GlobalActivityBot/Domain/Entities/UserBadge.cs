namespace GlobalActivityBot.Domain.Entities;

public class UserBadge
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BadgeId { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Badge Badge { get; set; } = null!;
}
