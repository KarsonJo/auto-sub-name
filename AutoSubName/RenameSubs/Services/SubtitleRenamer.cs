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

public partial class SubtitleRenamer(ISubtitleLanguageDetector languageDetector, IMatcher matcher)
    : ISubtitleRenamer
{
    public HashSet<RenamePlan> RenameSubs(
        MediaFolder folder,
        string? namingPattern,
        LanguageFormat languageFormat
    )
    {
        namingPattern ??= DefaultNamingPattern;
        var usedParameters = GetUsedParameters(namingPattern);

        // TODO: Skip exact matches
        var matchResults = matcher.Match(folder.MediaFiles);

        HashSet<RenamePlan> plans = [];
        foreach (var match in matchResults)
        {
            if (match.Video is null)
            {
                continue;
            }

            var languageName = usedParameters.Contains("lang")
                ? languageDetector.GetLanguage(match.Subtitle.FullPath)
                : null;

            var newName = Smart.Format(
                namingPattern,
                new
                {
                    name = Path.GetFileNameWithoutExtension(match.Video.FileName),
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
                    ext = match.Subtitle.Extension,
                }
            );

            if (newName == match.Subtitle.FileName)
            {
                continue;
            }

            plans.Add(new(match.Subtitle.FileName, newName));
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
}
