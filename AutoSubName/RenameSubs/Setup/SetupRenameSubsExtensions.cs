using AutoSubName.RenameSubs.Data;
using AutoSubName.RenameSubs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoSubName.RenameSubs.Setup;

public static class SetupRenameSubsExtensions
{
    public static IServiceCollection AddRenameSubs(this IServiceCollection services)
    {
        services.AddScoped<IMediaFolderRepository, MediaFolderRepository>();
        services
            .AddScoped<ISubtitleLanguageDetector, SubtitleLanguageDetector>()
            .AddScoped<ISubtitleRenamer, SubtitleRenamer>()
            .AddScoped<IMatcher, Matcher>();

        return services;
    }
}
