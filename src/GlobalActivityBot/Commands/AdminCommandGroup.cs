using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using GlobalActivityBot.Common;
using GlobalActivityBot.Services.Interfaces;

namespace GlobalActivityBot.Commands;

[Command("admin")]
public class AdminCommandGroup
{
    private readonly IBadgeService _badgeService;
    private readonly IStatsService _statsService;

    public AdminCommandGroup(IBadgeService badgeService, IStatsService statsService)
    {
        _badgeService = badgeService;
        _statsService = statsService;
    }

    [Command("givebadge")]
    [Description("Award a badge to a user (admin only)")]
    public async Task GiveBadgeAsync(CommandContext ctx, DiscordUser target, string badgeName)
    {
        if (!PermissionGuards.IsGuildAdmin(ctx))
        {
            await ResponseHelper.RespondErrorAsync(ctx, "Permission Denied", "You need administrator permissions to use this command.");
            return;
        }

        var success = await _badgeService.GiveBadgeToUserAsync(target.Id, badgeName);
        if (success)
            await ResponseHelper.RespondSuccessAsync(ctx, "Badge Awarded", $"Successfully awarded **{badgeName}** to {target.Mention}.");
        else
            await ResponseHelper.RespondErrorAsync(ctx, "Failed", "Could not award the badge. Either the badge doesn't exist, the user hasn't been seen, or they already have it.");
    }

    [Command("syncxp")]
    [Description("Force sync global XP (admin only)")]
    public async Task SyncXpAsync(CommandContext ctx)
    {
        if (!PermissionGuards.IsGuildAdmin(ctx))
        {
            await ResponseHelper.RespondErrorAsync(ctx, "Permission Denied", "You need administrator permissions to use this command.");
            return;
        }

        await _statsService.SyncGlobalXpAsync();
        await ResponseHelper.RespondSuccessAsync(ctx, "XP Synced", "Global XP has been synced successfully.");
    }
}
