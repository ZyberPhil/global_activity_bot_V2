using GlobalActivityBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GlobalActivityBot.Infrastructure.Data;

public class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<UserStat> UserStats => Set<UserStat>();
    public DbSet<ChannelStat> ChannelStats => Set<ChannelStat>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.DiscordId).HasColumnName("discord_id").HasMaxLength(20).IsRequired();
            e.Property(u => u.Username).HasColumnName("username").IsRequired();
            e.Property(u => u.GlobalXp).HasColumnName("global_xp");
            e.Property(u => u.GlobalLevel).HasColumnName("global_level");
            e.Property(u => u.CreatedAt).HasColumnName("created_at");
            e.HasIndex(u => u.DiscordId).IsUnique();
        });

        modelBuilder.Entity<Guild>(e =>
        {
            e.ToTable("guilds");
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).HasColumnName("id");
            e.Property(g => g.DiscordId).HasColumnName("discord_id").HasMaxLength(20).IsRequired();
            e.Property(g => g.Name).HasColumnName("name").IsRequired();
            e.Property(g => g.CreatedAt).HasColumnName("created_at");
            e.HasIndex(g => g.DiscordId).IsUnique();
        });

        modelBuilder.Entity<UserStat>(e =>
        {
            e.ToTable("user_stats");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.UserId).HasColumnName("user_id");
            e.Property(s => s.GuildId).HasColumnName("guild_id");
            e.Property(s => s.Xp).HasColumnName("xp");
            e.Property(s => s.Level).HasColumnName("level");
            e.Property(s => s.MessageCount).HasColumnName("message_count");
            e.Property(s => s.LastMessageAt).HasColumnName("last_message_at");
            e.HasOne(s => s.User).WithMany(u => u.Stats).HasForeignKey(s => s.UserId);
            e.HasOne(s => s.Guild).WithMany(g => g.UserStats).HasForeignKey(s => s.GuildId);
            e.HasIndex(s => new { s.UserId, s.GuildId }).IsUnique();
        });

        modelBuilder.Entity<ChannelStat>(e =>
        {
            e.ToTable("channel_stats");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.GuildId).HasColumnName("guild_id");
            e.Property(c => c.DiscordChannelId).HasColumnName("discord_channel_id").IsRequired();
            e.Property(c => c.MessageCount).HasColumnName("message_count");
            e.Property(c => c.LastMessageAt).HasColumnName("last_message_at");
            e.HasOne(c => c.Guild).WithMany(g => g.ChannelStats).HasForeignKey(c => c.GuildId);
        });

        modelBuilder.Entity<Badge>(e =>
        {
            e.ToTable("badges");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("id");
            e.Property(b => b.Name).HasColumnName("name").IsRequired();
            e.Property(b => b.Description).HasColumnName("description").IsRequired();
            e.Property(b => b.Emoji).HasColumnName("emoji").IsRequired();
            e.Property(b => b.CreatedAt).HasColumnName("created_at");
            e.HasIndex(b => b.Name).IsUnique();
        });

        modelBuilder.Entity<UserBadge>(e =>
        {
            e.ToTable("user_badges");
            e.HasKey(ub => ub.Id);
            e.Property(ub => ub.Id).HasColumnName("id");
            e.Property(ub => ub.UserId).HasColumnName("user_id");
            e.Property(ub => ub.BadgeId).HasColumnName("badge_id");
            e.Property(ub => ub.AwardedAt).HasColumnName("awarded_at");
            e.HasOne(ub => ub.User).WithMany(u => u.UserBadges).HasForeignKey(ub => ub.UserId);
            e.HasOne(ub => ub.Badge).WithMany(b => b.UserBadges).HasForeignKey(ub => ub.BadgeId);
            e.HasIndex(ub => new { ub.UserId, ub.BadgeId }).IsUnique();
        });
    }
}
