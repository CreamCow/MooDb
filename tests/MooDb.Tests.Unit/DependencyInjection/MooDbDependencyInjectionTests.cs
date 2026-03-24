using Microsoft.Extensions.DependencyInjection;
using MooDb.DependencyInjection;

namespace MooDb.Tests.Unit.DependencyInjection;

public sealed class MooDbDependencyInjectionTests
{
    [Fact]
    public void AddMooDbFactory_WhenCalled_RegistersFactoryOptions()
    {
        var services = new ServiceCollection();

        services.AddMooDbFactory();

        var optionsDescriptor = services.SingleOrDefault(x =>
            x.ServiceType == typeof(MooDbFactoryOptions));

        Assert.NotNull(optionsDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, optionsDescriptor.Lifetime);
    }

    [Fact]
    public void AddMooDbFactory_WhenCalled_RegistersFactoryService()
    {
        var services = new ServiceCollection();

        services.AddMooDbFactory();

        var factoryDescriptor = services.SingleOrDefault(x =>
            x.ServiceType == typeof(IMooDbFactory));

        Assert.NotNull(factoryDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, factoryDescriptor.Lifetime);
    }

    [Fact]
    public void AddMooDbFactory_WithConfigure_AppliesConfiguredDefaults()
    {
        var services = new ServiceCollection();

        services.AddMooDbFactory(options =>
        {
            options.CommandTimeoutSeconds = 123;
            options.StrictAutoMapping = true;
        });

        var optionsDescriptor = services.Single(x =>
            x.ServiceType == typeof(MooDbFactoryOptions));

        var options = Assert.IsType<MooDbFactoryOptions>(optionsDescriptor.ImplementationInstance);

        Assert.Equal(123, options.CommandTimeoutSeconds);
        Assert.True(options.StrictAutoMapping);
    }
}