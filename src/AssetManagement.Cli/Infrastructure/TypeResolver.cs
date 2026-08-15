using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace AssetManagement.Cli.Infrastructure;

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    private readonly IServiceScope _scope = provider.CreateScope();

    public object? Resolve(Type? type)
        => type is null ? null : _scope.ServiceProvider.GetService(type) ?? Activator.CreateInstance(type);

    public void Dispose() => _scope.Dispose();
}
