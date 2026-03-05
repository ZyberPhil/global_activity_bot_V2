using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace GlobalActivityBot.Common;

public static class PermissionGuards
{
    public static bool IsGuildAdmin(CommandContext ctx)
    {
        if (ctx.Member is null) return false;
        return ctx.Member.Permissions.HasPermission(DiscordPermission.Administrator) ||
               ctx.Member.Permissions.HasPermission(DiscordPermission.ManageGuild);
    }
}
