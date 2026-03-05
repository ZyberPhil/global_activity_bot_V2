using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using GlobalActivityBot.Common;
using GlobalActivityBot.Services.Interfaces;

namespace GlobalActivityBot.Commands;

[Command("badge")]
public class BadgeCommandGroup
{
    private readonly IBadgeService _badgeService;

    public BadgeCommandGroup(IBadgeService badgeService)
    {
        _badgeService = badgeService;
    }

    [Command("list")]
    [Description("List all available badges")]
    public async Task ListAsync(CommandContext ctx)
    {
        var badges = await _badgeService.GetAllBadgesAsync();
        if (badges.Count == 0)
        {
            await ResponseHelper.RespondInfoAsync(ctx, "Badges", "No badges have been created yet.");
            return;
        }

        var lines = badges.Select(b => $"{b.Emoji} **{b.Name}** — {b.Description}");
        var embed = new DiscordEmbedBuilder()
            .WithTitle("🏅 Available Badges")
            .WithDescription(string.Join("\n", lines))
            .WithColor(EmbedFactory.ColorInfo)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await ResponseHelper.RespondWithEmbedAsync(ctx, embed);
    }

    [Command("mine")]
    [Description("View your badges")]
    public async Task MineAsync(CommandContext ctx)
    {
        var badges = await _badgeService.GetUserBadgesAsync(ctx.User.Id);
        if (badges.Count == 0)
        {
            await ResponseHelper.RespondInfoAsync(ctx, "Your Badges", "You have no badges yet.");
            return;
        }

        var lines = badges.Select(b => $"{b.Emoji} **{b.Name}** — {b.Description}");
        var embed = new DiscordEmbedBuilder()
            .WithTitle("🏅 Your Badges")
            .WithDescription(string.Join("\n", lines))
            .WithColor(EmbedFactory.ColorSuccess)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await ResponseHelper.RespondWithEmbedAsync(ctx, embed);
    }
}
