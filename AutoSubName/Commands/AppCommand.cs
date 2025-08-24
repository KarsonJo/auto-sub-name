using System.CommandLine;
using AutoSubName.RenameSubs.Features;
using AutoSubName.RenameSubs.Services;
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

        Option<bool> recursiveOption = new("--recursive", ["-r"])
        {
            Description = "Enable recursive directory scanning.",
            Required = false,
        };

        Option<string> namingPatternOption = new("--pattern", ["-p"])
        {
            Description = $"""
                Use a custom naming pattern. Defaults to "{ISubtitleRenamer.DefaultNamingPattern}".
                Possible variables: {string.Join(
                    ", ",
                    ISubtitleRenamer.PossibleVariables.Select(x => $"{{{x}}}")
                )}.
                The format follows axuno/SmartFormat interpolation syntax. 
                See https://github.com/axuno/SmartFormat/wiki/How-Formatters-Work.
                """,
            Required = false,
        };

        Option<ISubtitleRenamer.LanguageFormat> languageFormatOption = new(
            "--language-format",
            ["--lang-format", "-lf"]
        )
        {
            DefaultValueFactory = _ => ISubtitleRenamer.LanguageFormat.Ietf,
            Description = $"""
                The output language format.
                {ISubtitleRenamer.LanguageFormat.TwoLetter}: ISO 639-1 two-letter or ISO 639-3 three-letter code. e.g. "zh"
                {ISubtitleRenamer.LanguageFormat.ThreeLetter}: ISO 639-2 three-letter code. e.g. "zho"
                {ISubtitleRenamer.LanguageFormat.Ietf}: IETF BCP 47 language tag (RFC 4646). e.g. "zh-Hans"
                {ISubtitleRenamer.LanguageFormat.English}: language name in English.
                {ISubtitleRenamer.LanguageFormat.Native}: language name in the native language.
                {ISubtitleRenamer.LanguageFormat.Display}: language name in your system language.
                """,
            Required = false,
        };

        rootCommand.Options.Add(dirOption);
        rootCommand.Options.Add(recursiveOption);
        rootCommand.Options.Add(namingPatternOption);
        rootCommand.Options.Add(languageFormatOption);

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
                var command = new RenameSubtitles.DirectCall.Command()
                {
                    FolderPath = dir,
                    Recursive = parseResult.GetValue(recursiveOption),
                    CustomNamingPattern = parseResult.GetValue(namingPatternOption),
                    LanguageFormat = parseResult.GetValue(languageFormatOption),
                };
                await mediator.Send(command, ct);
            }
        );

        return rootCommand;
    }
}
