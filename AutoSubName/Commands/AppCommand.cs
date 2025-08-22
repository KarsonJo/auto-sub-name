using System.CommandLine;
using AutoSubName.RenameSubs.Features;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace AutoSubName.Commands;

public static class AppCommand
{
    /// <summary>
    /// Get the root command for the console application.
    /// </summary>
    /// <returns></returns>
    public static RootCommand Create(IServiceProvider provider)
    {
        RootCommand rootCommand = new("Bulk rename subtitles in a directory of video files.")!;

        Option<string> dirOption = new("--dir", ["-d"])
        {
            Description = "The directory to scan for subtitles. Defaults to the current directory.",
            Required = false,
        };
        rootCommand.Options.Add(dirOption);

        rootCommand.SetAction(
            async (parseResult, ct) =>
            {
                if (parseResult.GetValue(dirOption) is not string dir)
                {
                    dir = Directory.GetCurrentDirectory();
                }

                // Start the application
                using var scope = provider.CreateScope();

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var command = new RenameSubtitles.DirectCall.Command() { FolderPath = dir };
                await mediator.Send(command, ct);
            }
        );

        return rootCommand;
    }
}
