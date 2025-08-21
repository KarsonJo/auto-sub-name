using AutoSubName.RenameSubs.Entities;

namespace AutoSubName.RenameSubs.Data;

public interface IMediaFolderRepository
{
    public Task<MediaFolder> GetAsync(string folderPath, CancellationToken ct = default);
    public Task SaveChangesAsync(CancellationToken ct);
}
