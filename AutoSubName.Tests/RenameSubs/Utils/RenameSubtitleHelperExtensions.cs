using AutoSubName.RenameSubs.Features;
using AutoSubName.Tests.Utils;
using AutoSubName.Tests.Utils.Suts;

namespace AutoSubName.Tests.RenameSubs.Utils;

public static class RenameSubtitleHelperExtensions
{
    #region Dto Seeders
    public static RenameSubtitles.DirectCall.Command SeedRenameSubtitlesDirectCallCommand(
        this ISut sut,
        Action<RenameSubtitles.DirectCall.Command>? modifier = null
    )
    {
        return modifier.Modify(new() { FolderPath = sut.RootFileDirectory });
    }
    #endregion
}
