using System.Diagnostics;
using System.Text.RegularExpressions;
using AutoSubName.RenameSubs.Entities;
using static AutoSubName.RenameSubs.Services.IMatcher;

namespace AutoSubName.RenameSubs.Services;

public interface IMatcher
{
    public record Result
    {
        public MediaFile? Video { get; }
        public MediaFile Subtitle { get; }

        public Result(MediaFile? video, MediaFile subtitle)
        {
            Debug.Assert(video is null || video.Type == MediaType.Video);
            Debug.Assert(subtitle.Type == MediaType.Subtitle);
            Subtitle = subtitle;
            Video = video;
        }
    }

    public SortedSet<Result> Match(IEnumerable<MediaFile> files);
}

public class Matcher : IMatcher
{
    private readonly List<IModularMatcher> matchers =
    [
        new SeasonEpisodeMatcher(),
        new SeriesIdMatcher(),
        new FileOrderMatcher(),
    ];

    private static Comparer<Result> Comparer =>
        Comparer<Result>.Create(
            static (a, b) =>
            {
                var cmp = string.Compare(a.Video?.FileName, b.Video?.FileName);
                return cmp != 0 ? cmp : string.Compare(a.Subtitle.FileName, b.Subtitle.FileName);
            }
        );

    public SortedSet<Result> Match(IEnumerable<MediaFile> files)
    {
        var videos = files.Where(x => x.Type == MediaType.Video).ToList();
        var subtitles = files.Where(x => x.Type == MediaType.Subtitle).ToList();

        if (videos.Count == 0 || subtitles.Count == 0)
        {
            return [];
        }

        SortedSet<Result> results = new(Comparer);
        foreach (var matcher in matchers)
        {
            matcher.Match(videos, subtitles, results);

            if (subtitles.Count == 0)
            {
                break;
            }
        }

        // Filter out empty results
        results.RemoveWhere(x => x.Video is null);

        return results;
    }
}

public interface IModularMatcher
{
    /// <summary>
    /// Matches subtitles to videos. The videos and subtitles are assumed to be in the same folder.
    /// </summary>
    /// <param name="videos">Videos to match.</param>
    /// <param name="subtitles">Subtitle files to match. Matched subtitles will be removed from this list.</param>
    /// <param name="results">Newly matched subtitles will be added to this list.</param>
    public void Match(List<MediaFile> videos, List<MediaFile> subtitles, SortedSet<Result> results);
}

public abstract class KeywordMatcher<T> : IModularMatcher
    where T : notnull
{
    /// <inheritdoc />
    public void Match(List<MediaFile> videos, List<MediaFile> subtitles, SortedSet<Result> results)
    {
        // Map keyword to video
        Dictionary<T, MediaFile> videoMap = [];
        foreach (var video in videos)
        {
            var keyword = ExtractKeyword(video.FileName);
            if (keyword is not null)
            {
                videoMap[keyword] = video;
            }
        }

        if (videoMap.Count == 0)
        {
            return;
        }

        // Match subtitles
        for (int i = subtitles.Count - 1; i >= 0; i--)
        {
            var keyword = ExtractKeyword(subtitles[i].FileName);
            if (keyword is not null)
            {
                videoMap.TryGetValue(keyword, out var video);
                // Video may be null, but still should be matched.
                results.Add(new(video, subtitles[i]));
                subtitles.RemoveAt(i);
            }
        }
    }

    protected abstract T? ExtractKeyword(string fileName);
}

public partial class SeasonEpisodeMatcher : KeywordMatcher<string>
{
    protected override string? ExtractKeyword(string fileName)
    {
        var m = Episode().Match(fileName);
        return m.Success ? $"{int.Parse(m.Groups[1].Value)}-{int.Parse(m.Groups[2].Value)}" : null;
    }

    [GeneratedRegex(@"S(\d+)[\s\-_.]?E(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex Episode();
}

public partial class SeriesIdMatcher : KeywordMatcher<string>
{
    protected override string? ExtractKeyword(string fileName)
    {
        var m = SeriesId().Match(fileName);
        return m.Success ? m.Value : null;
    }

    [GeneratedRegex(@"[A-Z]{2,5}-\d{3,4}", RegexOptions.IgnoreCase)]
    private static partial Regex SeriesId();
}

public class FileOrderMatcher : IModularMatcher
{
    /// <summary>
    /// Matches subtitles to videos. The videos and subtitles are assumed to be in the same folder, having the same file count and in the same order.
    /// </summary>
    /// <inheritdoc/>
    public void Match(List<MediaFile> videos, List<MediaFile> subtitles, SortedSet<Result> results)
    {
        if (videos.Count != subtitles.Count)
        {
            return;
        }

        videos.Sort(
            static (a, b) => string.Compare(a.FileName, b.FileName, StringComparison.Ordinal)
        );
        subtitles.Sort(
            static (a, b) => string.Compare(a.FileName, b.FileName, StringComparison.Ordinal)
        );

        for (int i = videos.Count - 1; i >= 0; i--)
        {
            results.Add(new(videos[i], subtitles[i]));
            subtitles.RemoveAt(i);
        }
    }
}
