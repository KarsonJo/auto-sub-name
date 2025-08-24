namespace AutoSubName.RenameSubs.Entities;

public static class MediaFileExtensions
{
    // csharpier-ignore-start
    public static readonly HashSet<string> Video = ["webm", "mkv", "flv", "vob", "ogv", "ogg", "rrc", "gifv", "mng", "mov", "avi", "qt", "wmv", "yuv", "rm", "asf", "amv", "mp4", "m4p", "m4v", "mpg", "mp2", "mpeg", "mpe", "mpv", "m4v", "svi", "3gp", "3g2", "mxf", "roq", "nsv", "flv", "f4v", "f4p", "f4a", "f4b", "mod"];
    public static readonly HashSet<string> Subtitle = ["srt", "ass", "sub"];
    // csharpier-ignore-end
}

public record RenamePlan(string OldName, string NewName);

public partial class MediaFolder
{
    public virtual IList<MediaFile> MediaFiles { get; protected set; } = null!;

    public static MediaFolder Create(string folderPath)
    {
        var filePaths = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);

        List<MediaFile> mediaFiles = [];

        foreach (var filePath in filePaths)
        {
            var ext = Path.GetExtension(filePath).ToLower().TrimStart('.');

            if (MediaFileExtensions.Video.Contains(ext))
            {
                mediaFiles.Add(MediaFile.Create(filePath, MediaType.Video));
            }
            else if (MediaFileExtensions.Subtitle.Contains(ext))
            {
                mediaFiles.Add(MediaFile.Create(filePath, MediaType.Subtitle));
            }
        }

        return new() { MediaFiles = mediaFiles };
    }

    public void RenameSubs(IEnumerable<RenamePlan> renames)
    {
        var mediaFileDict = MediaFiles.ToDictionary(x => x.FileName, x => x);

        foreach (var (oldName, newName) in renames)
        {
            var mediaFile = mediaFileDict[oldName];
            mediaFile.Rename(newName);
        }
    }
}

public enum MediaType
{
    Video,
    Subtitle,
}

public class MediaFile
{
    public virtual string FullPath { get; protected set; } = null!;
    public virtual string FileName { get; protected set; } = null!;
    public virtual string Extension { get; protected set; } = null!;
    public virtual MediaType Type { get; protected set; }

    public static MediaFile Create(string fullPath, MediaType type)
    {
        return new MediaFile()
        {
            FullPath = fullPath,
            FileName = Path.GetFileName(fullPath),
            Extension = GetExtension(fullPath),
            Type = type,
        };
    }

    public void Rename(string newName)
    {
        var ext = GetExtension(newName);
        if (ext != Extension)
        {
            throw new InvalidOperationException("Extension mismatch");
        }
        FullPath = Path.Combine(Path.GetDirectoryName(FullPath)!, newName);
        FileName = newName;
    }

    private static string GetExtension(string name)
    {
        return Path.GetExtension(name).ToLower().TrimStart('.');
    }
}

public class UnsupportedNamingParameterException(
    string namingParameter,
    string message = "Not supported naming parameter"
) : Exception($"{message}: {namingParameter}")
{
    public string NamingParameter => namingParameter;
}
