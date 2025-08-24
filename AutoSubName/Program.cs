using AutoSubName.Commands;
using AutoSubName.RenameSubs.Data;
using AutoSubName.RenameSubs.Services;
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
        services.AddScoped<ISubtitleLanguageDetector, SubtitleLanguageDetector>();
        services.AddScoped<ISubtitleRenamer, SubtitleRenamer>();

        return services;
    }

    /// <summary>
    /// Entry point for the console application.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static Task<int> Main(string[] args)
    {
        var services = CreateAppService().BuildServiceProvider();
        return AppCommand.Create(services).Parse(args).InvokeAsync();
    }
}
