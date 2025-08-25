using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static AutoSubName.RenameSubs.Services.ISubtitleLanguageDetector;

namespace AutoSubName.RenameSubs.Services;

public interface ISubtitleLanguageDetector
{
    /// <summary>
    /// Supplement for regions that use region specific language codes.
    /// </summary>
    public static readonly Dictionary<string, HashSet<string>> NeutralLangAliases = new()
    {
        ["zh-Hans"] = ["zh-CN", "zh-SG"],
        ["zh-Hant"] = ["zh-TW", "zh-HK", "zh-MO"],
    };

    // csharpier-ignore-start
    public static readonly List<string> DefaultLangaugesTags = ["en", "zh-Hans", "zh-Hant", "ja", "ko", "es", "fr", "de", "ru", "pt", "ar"];
    // csharpier-ignore-end

    public void SetSupportedLanguageTags(IEnumerable<string>? languageTags);
    public CultureInfo? GetLanguage(string fullPath);
}

public class SubtitleLanguageDetector : ISubtitleLanguageDetector
{
    private readonly Dictionary<string, CultureInfo> LangAliases = new(
        StringComparer.OrdinalIgnoreCase
    );
    private Regex? LangRegex { get; set; }

    public void SetSupportedLanguageTags(IEnumerable<string>? languageTags)
    {
        var supportedCultures = (languageTags ??= DefaultLangaugesTags)
            .Select(x => new CultureInfo(x))
            .OrderBy(x => x.LCID);

        LangAliases.Clear();
        foreach (var culture in supportedCultures)
        {
            LangAliases.TryAdd(culture.Name.ToLower(), culture); // en-us
            LangAliases.TryAdd(culture.TwoLetterISOLanguageName.ToLower(), culture); // en
            LangAliases.TryAdd(culture.ThreeLetterISOLanguageName.ToLower(), culture); // eng
            LangAliases.TryAdd(culture.ThreeLetterWindowsLanguageName.ToLower(), culture); // enu

            if (!string.IsNullOrWhiteSpace(culture.Parent.Name))
            {
                LangAliases.TryAdd(culture.Parent.Name.ToLower(), culture);
            }

            if (NeutralLangAliases.TryGetValue(culture.Name, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    LangAliases.TryAdd(alias, culture);
                }
            }
        }

        // remove the invalid windows language name.
        LangAliases.Remove("zzz");
        LangAliases.Remove("");

        // en-us = en-gb > eng > en
        var sortedLangs = LangAliases.Keys.OrderByDescending(l => l.Length).ToList();

        var pattern = $@"(?<=^|[^a-zA-Z0-9])({string.Join("|", sortedLangs)})(?=$|[^a-zA-Z0-9])";
        LangRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public CultureInfo? GetLanguage(string fullPath)
    {
        var language = GetLanguageFromFileName(fullPath);

        return language;
    }

    private CultureInfo? GetLanguageFromFileName(string fullPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(fullPath);

        if (LangRegex is null)
        {
            SetSupportedLanguageTags(null);
        }
        Debug.Assert(LangRegex is not null, "LangRegex should not be null here.");

        var match = LangRegex.Match(fileName);

        if (!match.Success)
        {
            return null;
        }

        return LangAliases[match.Groups[1].Value];
    }
}
