using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoSubName.RenameSubs.Services;

public interface ISubtitleLanguageDetector
{
    public CultureInfo? GetLanguage(string fullPath);
}

public class SubtitleLanguageDetector : ISubtitleLanguageDetector
{
    private static readonly Dictionary<string, CultureInfo> langAliases = new(
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly Regex LangRegex;

    /// <summary>
    /// Supplement for regions that use region specific language codes.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> NeutralLangAliases = new()
    {
        ["zh-Hans"] = ["zh-CN", "zh-SG"],
        ["zh-Hant"] = ["zh-TW", "zh-HK", "zh-MO"],
    };

    static SubtitleLanguageDetector()
    {
        var langs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var supportedCultures = CultureInfo
            .GetCultures(CultureTypes.NeutralCultures)
            .OrderBy(x => x.LCID);
        foreach (var culture in supportedCultures)
        {
            langAliases.TryAdd(culture.Name.ToLower(), culture); // en-us
            langAliases.TryAdd(culture.TwoLetterISOLanguageName.ToLower(), culture); // en
            langAliases.TryAdd(culture.ThreeLetterISOLanguageName.ToLower(), culture); // eng
            langAliases.TryAdd(culture.ThreeLetterWindowsLanguageName.ToLower(), culture); // enu

            if (!string.IsNullOrWhiteSpace(culture.Parent.Name))
            {
                langAliases.TryAdd(culture.Parent.Name.ToLower(), culture);
            }

            if (NeutralLangAliases.TryGetValue(culture.Name, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    langAliases.TryAdd(alias, culture);
                }
            }
        }

        // remove the invalid windows language name.
        langAliases.Remove("zzz");
        langAliases.Remove("");

        // en-us = en-gb > eng > en
        var sortedLangs = langAliases.Keys.OrderByDescending(l => l.Length).ToList();

        var pattern = $@"(?<=^|[^a-zA-Z0-9])({string.Join("|", sortedLangs)})(?=$|[^a-zA-Z0-9])";
        LangRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public CultureInfo? GetLanguage(string fullPath)
    {
        var language = GetLanguageFromFileName(fullPath);

        return language;
    }

    private static CultureInfo? GetLanguageFromFileName(string fullPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(fullPath);

        var match = LangRegex.Match(fileName);

        if (!match.Success)
        {
            return null;
        }

        return langAliases[match.Groups[1].Value];
    }
}
