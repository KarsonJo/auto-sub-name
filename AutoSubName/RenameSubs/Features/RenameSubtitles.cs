using AutoSubName.RenameSubs.Data;
using AutoSubName.RenameSubs.Services;
using Mediator;

namespace AutoSubName.RenameSubs.Features;

public static class RenameSubtitles
{
    public static class DirectCall
    {
        public class Command : IRequest
        {
            public string FolderPath { get; set; } = null!;
            public bool Recursive { get; set; }
        }

        public class Handler(
            IMediaFolderRepository repository,
            ISubtitleLanguageDetector languageDetector
        ) : IRequestHandler<Command>
        {
            public async ValueTask<Unit> Handle(
                Command request,
                CancellationToken cancellationToken
            )
            {
                if (request.Recursive)
                {
                    await Traverse(request.FolderPath);
                }
                else
                {
                    await RenameSubsInTopDirectory(request.FolderPath);
                }

                return default;

                async Task RenameSubsInTopDirectory(string path)
                {
                    var folder = await repository.GetAsync(path, cancellationToken);

                    folder.RenameSubs(languageDetector);

                    await repository.SaveChangesAsync(cancellationToken);
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
