using AutoSubName.RenameSubs.Data;
using AutoSubName.RenameSubs.Services;
using Mediator;
using Microsoft.Extensions.Logging;

namespace AutoSubName.RenameSubs.Features;

public static class RenameSubtitles
{
    public static class DirectCall
    {
        public class Command : IRequest
        {
            public string FolderPath { get; set; } = null!;
            public bool Recursive { get; set; }
            public string? CustomNamingPattern { get; set; }
            public ISubtitleRenamer.LanguageFormat LanguageFormat { get; set; }
            public bool DryRun { get; set; }
        }

        public class Handler(
            ILogger<Handler> logger,
            IMediaFolderRepository repository,
            ISubtitleRenamer subtitleRenamer
        ) : IRequestHandler<Command>
        {
            public async ValueTask<Unit> Handle(
                Command request,
                CancellationToken cancellationToken
            )
            {
                int renamed = 0;

                logger.LogInformation(
                    "Scanning subtitles in {FolderPath}. This may take a while...",
                    request.FolderPath
                );

                if (request.Recursive)
                {
                    await Traverse(request.FolderPath);
                }
                else
                {
                    await RenameSubsInTopDirectory(request.FolderPath);
                }

                if (renamed == 0)
                {
                    logger.LogInformation("No subtitles were renamed.");
                    if (!request.Recursive)
                    {
                        logger.LogInformation(
                            "Note: Use --recursive or -r to scan subtitles in subdirectories."
                        );
                    }
                }
                else
                {
                    if (request.DryRun)
                    {
                        logger.LogInformation("Would rename {Renamed} subtitles.", renamed);
                    }
                    else
                    {
                        logger.LogInformation("Renamed {Renamed} subtitles.", renamed);
                    }
                }

                return default;

                async Task RenameSubsInTopDirectory(string path)
                {
                    if (request.DryRun)
                    {
                        logger.LogInformation("Renaming subtitles in top directory {Path}.", path);
                    }
                    else
                    {
                        logger.LogDebug("Renaming subtitles in top directory {Path}.", path);
                    }

                    var folder = await repository.GetAsync(path, cancellationToken);

                    var plans = subtitleRenamer.RenameSubs(
                        folder,
                        request.CustomNamingPattern,
                        request.LanguageFormat
                    );

                    if (request.DryRun)
                    {
                        foreach (var plan in plans)
                        {
                            logger.LogInformation(
                                "Would rename {OldName} to {NewName}.",
                                plan.OldName,
                                plan.NewName
                            );
                        }

                        renamed += folder.RenameSubs(plans);

                        logger.LogInformation(
                            "Would rename {Renamed} subtitles in {Path}.",
                            renamed,
                            path
                        );
                    }
                    else
                    {
                        if (logger.IsEnabled(LogLevel.Trace))
                        {
                            foreach (var plan in plans)
                            {
                                logger.LogTrace(
                                    "Will rename {OldName} to {NewName}.",
                                    plan.OldName,
                                    plan.NewName
                                );
                            }
                        }

                        renamed += folder.RenameSubs(plans);

                        await repository.SaveChangesAsync(cancellationToken);

                        logger.LogDebug("Renamed {Renamed} subtitles in {Path}.", renamed, path);
                    }
                }

                async Task Traverse(string path)
                {
                    await RenameSubsInTopDirectory(path);

                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        await Traverse(dir);
                    }
                }
            }
        }
    }
}
