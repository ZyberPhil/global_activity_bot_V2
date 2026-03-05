using System.ComponentModel;
using DSharpPlus.Commands;

namespace GlobalActivityBot.Commands;

public class PingCommand
{
    [Command("ping")]
    [Description("Check if the bot is alive")]
    public static async Task PingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("🏓 Pong!");
    }
}
