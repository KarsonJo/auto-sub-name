using System.Text.RegularExpressions;
using AutoSubName.RenameSubs.Entities;
using SmartFormat;
using static AutoSubName.RenameSubs.Services.ISubtitleRenamer;

namespace AutoSubName.RenameSubs.Services;

public interface ISubtitleRenamer
{
    public static readonly string DefaultNamingPattern = "{name}{lang:.{}|}.{ext}";
    public static HashSet<string> PossibleVariables => ["name", "lang", "ext"];

    public enum LanguageFormat
    {
        TwoLetter,
        ThreeLetter,
        Ietf,
        English,
        Native,
        Display,
    }

    public HashSet<RenamePlan> RenameSubs(
        MediaFolder folder,
        string? namingPattern,
        LanguageFormat languageFormat
    );
}

public partial class SubtitleRenamer(ISubtitleLanguageDetector languageDetector) : ISubtitleRenamer
{
    public HashSet<RenamePlan> RenameSubs(
        MediaFolder folder,
        string? namingPattern,
        LanguageFormat languageFormat
    )
    {
        namingPattern ??= DefaultNamingPattern;
        var usedParameters = GetUsedParameters(namingPattern);

        var subtitleFiles = folder.MediaFiles.Where(x => x.Type == MediaType.Subtitle).ToHashSet();

        HashSet<RenamePlan> plans = [];
        foreach (var subtitleFile in subtitleFiles)
        {
            // TODO: Skip exact matches
            string? episode = ExtractEpisode(subtitleFile.FileName);

            if (episode == null)
            {
                continue;
            }

            // TODO: Make this more efficient
            var matchedVideo = folder.MediaFiles.FirstOrDefault(x =>
                x.Type == MediaType.Video
                && x.FileName.Contains(episode, StringComparison.OrdinalIgnoreCase)
            );

            if (matchedVideo == null)
            {
                continue;
            }

            var languageName = usedParameters.Contains("lang")
                ? languageDetector.GetLanguage(subtitleFile.FullPath)
                : null;

            var newName = Smart.Format(
                namingPattern,
                new
                {
                    name = Path.GetFileNameWithoutExtension(matchedVideo.FileName),
                    lang = languageName is null
                        ? null
                        : languageFormat switch
                        {
                            LanguageFormat.TwoLetter => languageName.TwoLetterISOLanguageName,
                            LanguageFormat.ThreeLetter => languageName.ThreeLetterISOLanguageName,
                            LanguageFormat.Ietf => languageName.IetfLanguageTag,
                            LanguageFormat.English => languageName.EnglishName,
                            LanguageFormat.Native => languageName.NativeName,
                            LanguageFormat.Display => languageName.DisplayName,
                            _ => throw new InvalidOperationException(
                                "Language format is required when using language parameter."
                            ),
                        },
                    ext = subtitleFile.Extension,
                }
            );

            plans.Add(new(subtitleFile.FileName, newName));
        }

        return plans;
    }

    private static HashSet<string> GetUsedParameters(string namingPattern)
    {
        HashSet<string> usedParameters = [];
        foreach (Match parameter in NamingPatternParameter().Matches(namingPattern))
        {
            var name = parameter.Groups[1].Value;
            if (!PossibleVariables.Contains(name))
            {
                throw new UnsupportedNamingParameterException(name);
            }
            usedParameters.Add(name);
        }
        return usedParameters;
    }

    [GeneratedRegex(@"\{([a-zA-Z0-9]+)(.*?)\}")]
    private static partial Regex NamingPatternParameter();

    private static string? ExtractEpisode(string fileName)
    {
        var m = Episode().Match(fileName);
        return m.Success ? m.Value : null;
    }

    [GeneratedRegex(@"S\d{2}E\d{2}", RegexOptions.IgnoreCase)]
    private static partial Regex Episode();
}
