using AutoSubName.RenameSubs.Data;
using AutoSubName.RenameSubs.Features;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace AutoSubName;

public static class Program
{
    /// <summary>
    /// All core application logic is defined here.
    /// </summary>
    /// <returns></returns>
    public static ServiceCollection CreateAppService()
    {
        ServiceCollection services = new();

        services.AddMediator(o =>
        {
            o.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddScoped<IMediaFolderRepository, MediaFolderRepository>();

        return services;
    }

    /// <summary>
    /// Entry point for the console application.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task Main(string[] args)
    {
        // Create the service collection
        var services = CreateAppService();

        // Build the service provider
        var provider = services.BuildServiceProvider();

        // Start the application
        using var scope = provider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new RenameSubtitles.DirectCall.Command() { FolderPath = "" });
    }
}
