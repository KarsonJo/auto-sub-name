using AutoSubName.Tests.Utils.Suts;

namespace AutoSubName.Tests.RenameSubs.Utils;

static class FileHelperExtensions
{
    #region Dto Seeders
    const int KB = 1024;

    public static Stream SeedStream(this ISut _, int seed = 0, int sizeInKb = 1)
    {
        /**
         * https://stackoverflow.com/questions/4432178/creating-a-random-file-in-c-sharp
         */

        const int blockSize = KB;
        const int blocksPerKb = KB / blockSize;
        byte[] data = new byte[blockSize];
        Random rng = new(seed);

        MemoryStream stream = new();
        for (int i = 0; i < sizeInKb * blocksPerKb; i++)
        {
            rng.NextBytes(data);
            stream.Write(data, 0, data.Length);
        }
        stream.Position = 0;
        return stream;
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Compare two streams without changing positions.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool EqualsTo(this Stream? a, Stream? b)
    {
        /* References:
         * 1. https://gist.github.com/sebingel/447e2b86be27f6172bfe395b52d05c96
         * 2. https://stackoverflow.com/questions/1358510/how-to-compare-2-files-fast-using-net
         */

        if (a == b)
            return true;

        if (a == null || b == null)
            throw new ArgumentNullException(a == null ? nameof(a) : nameof(b));

        if (a.Length != b.Length)
            return false;

        const int BYTES_TO_READ = sizeof(long);

        int iterations = (int)Math.Ceiling((double)a.Length / BYTES_TO_READ);

        byte[] one = new byte[BYTES_TO_READ];
        byte[] two = new byte[BYTES_TO_READ];

        // Preserve positions.
        long posA = -1;
        long posB = -1;
        if (a.CanSeek)
            posA = a.Position;
        if (b.CanSeek)
            posB = b.Position;

        try
        {
            if (a.CanSeek)
                a.Position = 0;
            if (b.CanSeek)
                b.Position = 0;

            for (int i = 0; i < iterations; i++)
            {
                a.Read(one, 0, BYTES_TO_READ);
                b.Read(two, 0, BYTES_TO_READ);

                if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                    return false;
            }
        }
        finally
        {
            if (a.CanSeek)
                a.Position = posA;
            if (b.CanSeek)
                b.Position = posB;
        }

        return true;
    }

    public static async Task<string> CreateSubtitleFileAsync(
        this ISut sut,
        Stream? content = null,
        string? fileName = null,
        string extension = "srt",
        string? basePath = null
    )
    {
        return await sut.CreateFileAsync(content, fileName, extension, basePath);
    }

    public static async Task<string> CreateVideoFileAsync(
        this ISut sut,
        Stream? content = null,
        string? fileName = null,
        string extension = "mp4",
        string? basePath = null
    )
    {
        return await sut.CreateFileAsync(content, fileName, extension, basePath);
    }

    public static async Task<string> CreateFileAsync(
        this ISut sut,
        Stream? content = null,
        string? fileName = null,
        string extension = "mp4",
        string? basePath = null
    )
    {
        string fileNameWithExt = $"{fileName ?? Guid.NewGuid().ToString()}.{extension}";
        string path = Path.Combine(basePath ?? sut.RootFileDirectory, fileNameWithExt);

        using var file = File.Create(path);
        await (content ?? sut.SeedStream()).CopyToAsync(file);

        return fileNameWithExt;
    }

    public static bool FileExists(this ISut sut, string fileNameWithExt, string? basePath = null)
    {
        basePath ??= sut.RootFileDirectory;
        return File.Exists(Path.Combine(basePath, fileNameWithExt));
    }
    #endregion
}
