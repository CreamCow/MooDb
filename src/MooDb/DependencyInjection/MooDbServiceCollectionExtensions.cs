using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MooDb.DependencyInjection;

/// <summary>
/// Provides dependency injection registration helpers for MooDb.
/// </summary>
public static class MooDbServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MooDb factory services using default factory options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection instance.</returns>
    public static IServiceCollection AddMooDbFactory(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(new MooDbFactoryOptions());
        services.TryAddSingleton<IMooDbFactory, MooDbFactory>();

        return services;
    }

    /// <summary>
    /// Registers the MooDb factory services and allows default factory options to be configured.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration callback for factory defaults.</param>
    /// <returns>The same service collection instance.</returns>
    public static IServiceCollection AddMooDbFactory(
        this IServiceCollection services,
        Action<MooDbFactoryOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MooDbFactoryOptions();
        configure(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IMooDbFactory, MooDbFactory>();

        return services;
    }
}