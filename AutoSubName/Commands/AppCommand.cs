using System.CommandLine;
using System.Globalization;
using AutoSubName.RenameSubs.Features;
using AutoSubName.RenameSubs.Services;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoSubName.Commands;

public static class AppCommand
{
    /// <summary>
    /// Get the root command for the console application.
    /// </summary>
    /// <returns></returns>
    public static RootCommand Create(Program.CreateAppResult host)
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

        Option<bool> verboseOption = new("--verbose", ["-v"])
        {
            Description = "Enable verbose logging.",
            Required = false,
        };

        Option<bool> dryRunOption = new("--dry-run", ["-n"])
        {
            Description = "Scan and output possible changes, but don't rename anything.",
            Required = false,
        };

        Option<List<string>> languagesOption = new("--languages", ["-l"])
        {
            DefaultValueFactory = _ => ISubtitleLanguageDetector.DefaultLangaugesTags,
            Description = $"The subtitle languages to detect in ISO 639 or IETF BCP 47 format.",
            Required = false,
        };

        rootCommand.Options.Add(dirOption);
        rootCommand.Options.Add(recursiveOption);
        rootCommand.Options.Add(namingPatternOption);
        rootCommand.Options.Add(languageFormatOption);
        rootCommand.Options.Add(verboseOption);
        rootCommand.Options.Add(dryRunOption);
        rootCommand.Options.Add(languagesOption);

        rootCommand.SetAction(
            async (parseResult, ct) =>
            {
                // Set working directory
                if (parseResult.GetValue(dirOption) is not string dir)
                {
                    dir = Directory.GetCurrentDirectory();
                }

                // Override logging level
                if (parseResult.GetValue(verboseOption))
                {
                    host.LoggingSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                }

                // Set languages
                if (parseResult.GetValue(languagesOption) is List<string> languages)
                {
                    var srv = host.App.Services.GetRequiredService<ISubtitleLanguageDetector>();
                    try
                    {
                        srv.SetSupportedLanguageTags(
                            languages.Select(x => x.Split(['|', ','])).SelectMany(x => x).Distinct()
                        );
                    }
                    catch (CultureNotFoundException ex)
                    {
                        var logger = host.App.Services.GetRequiredService<
                            ILogger<ISubtitleLanguageDetector>
                        >();
                        logger.LogError(
                            "The provided language {Language} is not supported.",
                            ex.InvalidCultureName
                        );
                        return 1;
                    }
                }

                // Send the command
                using var scope = host.App.Services.CreateScope();

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var command = new RenameSubtitles.DirectCall.Command()
                {
                    FolderPath = dir,
                    Recursive = parseResult.GetValue(recursiveOption),
                    CustomNamingPattern = parseResult.GetValue(namingPatternOption),
                    LanguageFormat = parseResult.GetValue(languageFormatOption),
                    DryRun = parseResult.GetValue(dryRunOption),
                };
                await mediator.Send(command, ct);

                return 0;
            }
        );

        return rootCommand;
    }
}
