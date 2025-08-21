using AutoSubName.Tests.Utils.TestApp.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace AutoSubName.Tests.Utils.TestApp;

[CollectionDefinition(nameof(TestResourceManager))]
public class TestAppCreatorCollection : ICollectionFixture<TestResourceManager> { }

public sealed class TestResourceManager : IAsyncLifetime
{
    private ServiceProvider? testResourceProvider;

    private ServiceProvider TestResourceProvider
    {
        get
        {
            return testResourceProvider
                ?? throw new InvalidOperationException(
                    $"{nameof(IAsyncLifetime)}.{nameof(InitializeAsync)} must be called before accessing test resources. {nameof(TestResourceManager)} should only be used in xUnit test fixtures."
                );
        }
        set { testResourceProvider = value; }
    }

    public Task<T> GetResource<T>()
        where T : ITestResource, new()
    {
        return TestResourceProvider.GetRequiredService<LazyAsyncResource<T>>().Resource;
    }

    public ValueTask InitializeAsync()
    {
        var serviceCollection = new ServiceCollection();

        // Add all test resources.
        var type = typeof(ITestResource);
        var types = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);

        foreach (var resourceType in types)
        {
            serviceCollection.AddSingleton(
                typeof(LazyAsyncResource<>).MakeGenericType(resourceType)
            );
        }

        TestResourceProvider = serviceCollection.BuildServiceProvider();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return TestResourceProvider.DisposeAsync();
    }

    class LazyAsyncResource<T> : IAsyncDisposable
        where T : ITestResource, new()
    {
        private readonly Lazy<Task<T>> resource;

        public Task<T> Resource => resource.Value;

        public LazyAsyncResource() => resource = new Lazy<Task<T>>(CreateAsync);

        private static async Task<T> CreateAsync()
        {
            var resource = new T();
            await resource.InitializeAsync();
            return resource;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);

            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (resource.IsValueCreated)
            {
                var a = await resource.Value;
                await a.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
