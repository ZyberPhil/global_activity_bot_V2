using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using GlobalActivityBot.Common;
using GlobalActivityBot.Services.Interfaces;

namespace GlobalActivityBot.Commands;

[Command("top")]
public class TopCommandGroup
{
    private readonly IStatsService _statsService;

    public TopCommandGroup(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [Command("server")]
    [Description("Show the server XP leaderboard")]
    public async Task ServerAsync(CommandContext ctx, int page = 1)
    {
        var guild = ctx.Guild;
        if (guild is null)
        {
            await ResponseHelper.RespondErrorAsync(ctx, "Error", "This command can only be used in a server.");
            return;
        }

        var leaderboard = await _statsService.GetGuildLeaderboardAsync(guild.Id, page);
        if (leaderboard.Count == 0)
        {
            await ResponseHelper.RespondInfoAsync(ctx, "Server Leaderboard", "No activity recorded yet.");
            return;
        }

        var lines = leaderboard.Select((entry, i) =>
            $"`{(page - 1) * 10 + i + 1}.` **{entry.User.Username}** — Level {entry.Stat.Level} ({entry.Stat.Xp} XP)");

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"🏆 Server Leaderboard — Page {page}")
            .WithDescription(string.Join("\n", lines))
            .WithColor(EmbedFactory.ColorInfo)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await ResponseHelper.RespondWithEmbedAsync(ctx, embed);
    }

    [Command("global")]
    [Description("Show the global XP leaderboard")]
    public async Task GlobalAsync(CommandContext ctx, int page = 1)
    {
        var leaderboard = await _statsService.GetGlobalLeaderboardAsync(page);
        if (leaderboard.Count == 0)
        {
            await ResponseHelper.RespondInfoAsync(ctx, "Global Leaderboard", "No activity recorded yet.");
            return;
        }

        var lines = leaderboard.Select((entry, i) =>
            $"`{(page - 1) * 10 + i + 1}.` **{entry.User.Username}** — Level {entry.User.GlobalLevel} ({entry.GlobalXp} XP)");

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"🌍 Global Leaderboard — Page {page}")
            .WithDescription(string.Join("\n", lines))
            .WithColor(EmbedFactory.ColorInfo)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await ResponseHelper.RespondWithEmbedAsync(ctx, embed);
    }
}
