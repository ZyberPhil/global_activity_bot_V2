using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using GlobalActivityBot.Common;
using GlobalActivityBot.Services.Interfaces;

namespace GlobalActivityBot.Commands;

public class GuildCommand
{
    private readonly IGuildService _guildService;
    private readonly IStatsService _statsService;

    public GuildCommand(IGuildService guildService, IStatsService statsService)
    {
        _guildService = guildService;
        _statsService = statsService;
    }

    [Command("guild")]
    [Description("Show server information and activity stats")]
    public async Task GuildInfoAsync(CommandContext ctx)
    {
        var guild = ctx.Guild;
        if (guild is null)
        {
            await ResponseHelper.RespondErrorAsync(ctx, "Error", "This command can only be used in a server.");
            return;
        }

        var dbGuild = await _guildService.GetOrCreateGuildAsync(guild.Id, guild.Name);
        var topEntries = await _statsService.GetGuildLeaderboardAsync(guild.Id, page: 1, pageSize: 3);

        var topStr = topEntries.Count > 0
            ? string.Join("\n", topEntries.Select((e, i) => $"`{i + 1}.` **{e.User.Username}** — {e.Stat.Xp} XP"))
            : "No activity yet.";

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"🏠 {guild.Name}")
            .AddField("Members", guild.MemberCount.ToString(), true)
            .AddField("Registered Since", dbGuild.CreatedAt.ToString("yyyy-MM-dd"), true)
            .AddField("Top Members", topStr, false)
            .WithColor(EmbedFactory.ColorInfo)
            .WithThumbnail(guild.IconUrl)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await ResponseHelper.RespondWithEmbedAsync(ctx, embed);
    }
}
