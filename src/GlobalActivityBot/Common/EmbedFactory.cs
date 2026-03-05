using DSharpPlus.Entities;

namespace GlobalActivityBot.Common;

public static class EmbedFactory
{
    public static readonly DiscordColor ColorSuccess = new DiscordColor(0x57F287);
    public static readonly DiscordColor ColorError = new DiscordColor(0xED4245);
    public static readonly DiscordColor ColorInfo = new DiscordColor(0x5865F2);

    public static DiscordEmbed Success(string title, string description)
        => new DiscordEmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(ColorSuccess)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

    public static DiscordEmbed Error(string title, string description)
        => new DiscordEmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(ColorError)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

    public static DiscordEmbed Info(string title, string description)
        => new DiscordEmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(ColorInfo)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
}
