using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MooDb.DependencyInjection;

/// <summary>
/// Provides dependency injection registration helpers for MooDbContext.
/// </summary>
public static class MooDbServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MooDbContext factory services using default factory options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection instance.</returns>
    public static IServiceCollection AddMooDbContextFactory(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(new MooDbContextFactoryOptions());
        services.TryAddSingleton<IMooDbContextFactory, MooDbContextFactory>();

        return services;
    }

    /// <summary>
    /// Registers the MooDbContext factory services and allows default factory options to be configured.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration callback for factory defaults.</param>
    /// <returns>The same service collection instance.</returns>
    public static IServiceCollection AddMooDbContextFactory(
        this IServiceCollection services,
        Action<MooDbContextFactoryOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MooDbContextFactoryOptions();
        configure(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IMooDbContextFactory, MooDbContextFactory>();

        return services;
    }
}