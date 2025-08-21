using AutoSubName.RenameSubs.Entities;
using ChangeTracking;

namespace AutoSubName.RenameSubs.Data;

public class MediaFolderRepository : IMediaFolderRepository
{
    private readonly List<MediaFolder> Folders = [];

    public Task<MediaFolder> GetAsync(string folderPath, CancellationToken ct = default)
    {
        var folder = MediaFolder.Create(folderPath);
        folder = folder.AsTrackable();
        Folders.Add(folder);
        return Task.FromResult(folder);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        for (int i = Folders.Count - 1; i >= 0; i--)
        {
            var folder = Folders[i];
            foreach (var file in folder.MediaFiles.CastToIChangeTrackableCollection().ChangedItems)
            {
                var trackableFile = file.CastToIChangeTrackable();
                var originalPath = trackableFile.GetOriginalValue(x => x.FullPath);

                if (originalPath != file.FullPath)
                {
                    File.Move(originalPath, file.FullPath, true);
                }

                trackableFile.AcceptChanges();
            }
            folder.CastToIChangeTrackable().AcceptChanges();
            Folders.RemoveAt(i);
        }
        return Task.CompletedTask;
    }
}
