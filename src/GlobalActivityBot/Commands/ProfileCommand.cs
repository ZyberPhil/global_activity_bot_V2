using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using GlobalActivityBot.Common;
using GlobalActivityBot.Services.Interfaces;

namespace GlobalActivityBot.Commands;

public class ProfileCommand
{
    private readonly IProfileService _profileService;

    public ProfileCommand(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [Command("profile")]
    [Description("View your activity profile")]
    public async Task ProfileAsync(CommandContext ctx, DiscordUser? target = null)
    {
        var targetUser = target ?? ctx.User;
        var guild = ctx.Guild;

        if (guild is null)
        {
            await ResponseHelper.RespondErrorAsync(ctx, "Error", "This command can only be used in a server.");
            return;
        }

        var profile = await _profileService.GetProfileAsync(targetUser.Id, guild.Id, guild.Name);
        if (profile == null)
        {
            await ResponseHelper.RespondInfoAsync(ctx, "Profile Not Found", $"{targetUser.Mention} has no activity yet.");
            return;
        }

        var badgeStr = profile.Badges.Count > 0
            ? string.Join(" ", profile.Badges.Select(b => b.Emoji))
            : "None";

        var guildXp = profile.GuildStat?.Xp ?? 0;
        var guildLevel = profile.GuildStat?.Level ?? 0;
        var guildMessages = profile.GuildStat?.MessageCount ?? 0;

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"📊 Profile — {profile.User.Username}")
            .WithColor(EmbedFactory.ColorInfo)
            .AddField("Global Level", $"{profile.User.GlobalLevel} ({profile.User.GlobalXp} XP)", true)
            .AddField("Server Level", $"{guildLevel} ({guildXp} XP)", true)
            .AddField("Server Messages", guildMessages.ToString(), true)
            .AddField("Badges", badgeStr, false)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await ResponseHelper.RespondWithEmbedAsync(ctx, embed);
    }
}
