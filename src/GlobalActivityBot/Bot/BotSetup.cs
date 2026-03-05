using GlobalActivityBot.BackgroundJobs;
using GlobalActivityBot.Infrastructure.Data;
using GlobalActivityBot.Services;
using GlobalActivityBot.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GlobalActivityBot.Bot;

public static class BotSetup
{
    public static IServiceCollection AddBotServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("Default")
            ?? configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException("No DB_CONNECTION_STRING configured. Set the DB_CONNECTION_STRING environment variable.");

        services.AddMemoryCache();
        services.AddDbContext<BotDbContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySql => mySql.EnableRetryOnFailure()));

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<IGuildService, GuildService>();
        services.AddScoped<IBadgeService, BadgeService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddHostedService<GlobalXpSyncJob>();

        return services;
    }
}
