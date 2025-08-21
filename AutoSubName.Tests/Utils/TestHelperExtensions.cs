using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using AutoSubName.Tests.Utils.Suts;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AutoSubName.Tests.Utils;

internal static class ActionExtensions
{
    public static T Modify<T>(this Action<T>? configure, T target)
    {
        configure?.Invoke(target);
        return target;
    }
}

internal static class ReflectionExtensions
{
    /// <summary>
    /// Private property setters for testing. (Modified by GPT to support nested properties)
    /// https://stackoverflow.com/questions/918341/unit-testing-private-setter-question-c
    /// </summary>
    public static void SetProperty<T, TValue>(
        this T instance,
        Expression<Func<T, TValue>> property,
        TValue value
    )
        where T : notnull
    {
        var memberExpr =
            GetMemberExpression(property.Body)
            ?? throw new ArgumentException(
                "Expression must be a property expression",
                nameof(property)
            );

        // Collect the property access chain into a stack
        var members = new Stack<MemberExpression>();
        while (memberExpr != null)
        {
            members.Push(memberExpr);
            memberExpr = GetMemberExpression(memberExpr.Expression);
        }

        // Traverse the chain to get the parent object of the last property
        object? current = instance;
        while (members.Count > 1)
        {
            var member = members.Pop();
            var prop = (PropertyInfo)member.Member;
            current = prop.GetValue(current);
            if (current == null)
            {
                throw new NullReferenceException(
                    $"Property {prop.Name} is null in the access chain."
                );
            }
        }

        // Set the final property value
        var lastMember = members.Pop();
        var lastProp = (PropertyInfo)lastMember.Member;
        lastProp.SetValue(current, value);
    }

    // Helper to unwrap expression body and get MemberExpression
    private static MemberExpression? GetMemberExpression(Expression? exp)
    {
        return exp switch
        {
            MemberExpression m => m,
            UnaryExpression u when u.Operand is MemberExpression m => m,
            _ => null,
        };
    }
}

public static class DependencyInjectionExtensions
{
    public abstract class Callable<T>(IServiceProvider serviceProvider)
        where T : notnull
    {
        public async Task Call(Func<T, Task> func)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await func(GetInstance(scope));
        }

        public async Task<TResult> Call<TResult>(Func<T, Task<TResult>> func)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            return await func(GetInstance(scope));
        }

        public async ValueTask Call(Func<T, ValueTask> func)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await func(GetInstance(scope));
        }

        public async ValueTask<TResult> Call<TResult>(Func<T, ValueTask<TResult>> func)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            return await func(GetInstance(scope));
        }

        public void Call(Action<T> action)
        {
            using var scope = serviceProvider.CreateScope();
            action(GetInstance(scope));
        }

        public TResult Call<TResult>(Func<T, TResult> func)
        {
            using var scope = serviceProvider.CreateScope();
            return func(GetInstance(scope));
        }

        protected abstract T GetInstance(IServiceScope scope);
    }

    private sealed class ActivatorServiceCreator<T>(IServiceProvider serviceProvider)
        : Callable<T>(serviceProvider)
        where T : notnull
    {
        protected override T GetInstance(IServiceScope scope)
        {
            return ActivatorUtilities.CreateInstance<T>(scope.ServiceProvider);
        }
    }

    public static Callable<T> Inject<T>(this ISut app)
        where T : notnull
    {
        return new ActivatorServiceCreator<T>(app.Services);
    }

    private sealed class ScopedServiceCreator<T>(IServiceProvider serviceProvider)
        : Callable<T>(serviceProvider)
        where T : notnull
    {
        protected override T GetInstance(IServiceScope scope)
        {
            return scope.ServiceProvider.GetRequiredService<T>();
        }
    }

    public static Callable<T> Scoped<T>(this ISut app)
        where T : notnull
    {
        return new ScopedServiceCreator<T>(app.Services);
    }

    public static AsyncServiceScope AsyncScope(this ISut app)
    {
        return app.Services.CreateAsyncScope();
    }

    public static T Service<T>(this ISut app)
        where T : notnull
    {
        return app.Services.GetRequiredService<T>();
    }

    public static T Service<T>(this IServiceScope serviceProvider)
        where T : notnull
    {
        return serviceProvider.ServiceProvider.GetRequiredService<T>();
    }

    public static ITestService<T> TestService<T>(this ISut app)
        where T : class
    {
        return app.Service<ITestService<T>>();
    }
}

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection Remove<T>(this IServiceCollection services)
    {
        if (services.IsReadOnly)
        {
            throw new ReadOnlyException($"{nameof(services)} is read only");
        }

        var serviceDescriptor = services.FirstOrDefault(descriptor =>
            descriptor.ServiceType == typeof(T)
        );
        if (serviceDescriptor != null)
            services.Remove(serviceDescriptor);

        return services;
    }

    public static IServiceCollection Remove(this IServiceCollection services, Type type)
    {
        if (services.IsReadOnly)
        {
            throw new ReadOnlyException($"{nameof(services)} is read only");
        }

        var serviceDescriptor = services.FirstOrDefault(descriptor =>
            descriptor.ServiceType == type
        );
        if (serviceDescriptor != null)
            services.Remove(serviceDescriptor);

        return services;
    }

    public static IServiceCollection ReplaceTestService<TReal>(this IServiceCollection services)
        where TReal : class
    {
        services.Remove<TReal>();

        var testMock = new Mock<ITestService<TReal>>();
        var serviceMock = testMock.As<TReal>();
        testMock.Setup(x => x.Mock).Returns(serviceMock);

        services.AddSingleton(testMock.Object);
        services.AddSingleton(serviceMock.Object);
        return services;
    }
}
