namespace AutoSubName.Tests.Utils.TestApp.Resources;

internal class TempDirectoryResource : ITestResource
{
    private string DirectoryRoot { get; } =
        Path.Combine(
            Path.GetTempPath(),
            System.Reflection.Assembly.GetCallingAssembly()!.GetName().Name!
        );
    public string DirectoryPath { get; private set; } = null!;

    public ValueTask InitializeAsync()
    {
        DirectoryPath = Path.Combine(
            DirectoryRoot,
            DateTime
                .Now.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
                .Replace(':', '.')
        );
        Directory.CreateDirectory(DirectoryPath);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(DirectoryRoot))
        {
            try
            {
                Directory.Delete(DirectoryRoot, recursive: true);
            }
            catch { }
        }
        return ValueTask.CompletedTask;
    }
}
