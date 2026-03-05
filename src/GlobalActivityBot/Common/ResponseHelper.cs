using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace GlobalActivityBot.Common;

public static class ResponseHelper
{
    public static async Task RespondWithEmbedAsync(CommandContext ctx, DiscordEmbed embed)
        => await ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed));

    public static async Task RespondSuccessAsync(CommandContext ctx, string title, string description)
        => await RespondWithEmbedAsync(ctx, EmbedFactory.Success(title, description));

    public static async Task RespondErrorAsync(CommandContext ctx, string title, string description)
        => await RespondWithEmbedAsync(ctx, EmbedFactory.Error(title, description));

    public static async Task RespondInfoAsync(CommandContext ctx, string title, string description)
        => await RespondWithEmbedAsync(ctx, EmbedFactory.Info(title, description));
}
