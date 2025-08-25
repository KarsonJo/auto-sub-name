using Microsoft.Extensions.DependencyInjection;

namespace AutoSubName.Tests.Utils.TestApp;

[Collection(nameof(TestResourceManager))]
public abstract class BasicSetup
{
    public static CancellationToken Cancellation => TestContext.Current.CancellationToken;
}

public abstract class BasicSetup<T> : BasicSetup
{
    public abstract T Sut { get; protected set; }
}

public abstract class ClassFixtureSetup<T> : BasicSetup<T>, IClassFixture<T>, IAsyncLifetime
    where T : class
{
    public sealed override T Sut { get; protected set; } = default!;

    public virtual async ValueTask InitializeAsync()
    {
        Sut = (await TestContext.Current.GetFixture<T>())!;
    }

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

public abstract class StandaloneSetup<T> : BasicSetup<T>, IAsyncLifetime
{
    public sealed override T Sut { get; protected set; } = default!;

    public virtual async ValueTask InitializeAsync()
    {
        Sut = await GetSutAsync();
        // Sut itself is not a fixture, so we have to initialize / dispose it manually.
        if (Sut is IAsyncLifetime asyncLifetime)
        {
            await asyncLifetime.InitializeAsync();
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (Sut is IAsyncLifetime asyncLifetime)
        {
            await asyncLifetime.DisposeAsync();
        }
    }

    private async Task<T> GetSutAsync()
    {
        var manager = await TestContext.Current.GetFixture<TestResourceManager>();
        return await GetSutAsync(manager!);
    }

    protected virtual Task<T> GetSutAsync(TestResourceManager manager)
    {
        var provider = new SimpleProvider();
        provider.Add(manager);
        return Task.FromResult(ActivatorUtilities.CreateInstance<T>(provider));
    }

    class SimpleProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _instances = [];

        public void Add<TService>(TService instance)
            where TService : class => _instances[typeof(TService)] = instance;

        public object? GetService(Type serviceType) =>
            _instances.TryGetValue(serviceType, out var instance) ? instance : null;
    }
}
