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
                var folder = await repository.GetAsync(request.FolderPath, cancellationToken);

                folder.RenameSubs(languageDetector);

                await repository.SaveChangesAsync(cancellationToken);

                return default;
            }
        }
    }
}
