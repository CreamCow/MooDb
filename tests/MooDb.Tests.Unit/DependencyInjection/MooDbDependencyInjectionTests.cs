using Microsoft.Extensions.DependencyInjection;
using MooDb.DependencyInjection;

namespace MooDb.Tests.Unit.DependencyInjection;

public sealed class MooDbDependencyInjectionTests
{
    [Fact]
    public void AddMooDbFactory_WhenCalled_RegistersFactoryOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMooDbContextFactory();

        var optionsDescriptor = services.SingleOrDefault(x =>
            x.ServiceType == typeof(MooDbContextFactoryOptions));

        // Assert
        Assert.NotNull(optionsDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, optionsDescriptor.Lifetime);
    }

    [Fact]
    public void AddMooDbFactory_WhenCalled_RegistersFactoryService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMooDbContextFactory();

        var factoryDescriptor = services.SingleOrDefault(x =>
            x.ServiceType == typeof(IMooDbContextFactory));

        // Assert
        Assert.NotNull(factoryDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, factoryDescriptor.Lifetime);
    }

    [Fact]
    public void AddMooDbFactory_WithConfigure_AppliesConfiguredDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMooDbContextFactory(options =>
        {
            options.CommandTimeoutSeconds = 123;
            options.StrictAutoMapping = true;
        });

        var optionsDescriptor = services.Single(x =>
            x.ServiceType == typeof(MooDbContextFactoryOptions));

        var options = Assert.IsType<MooDbContextFactoryOptions>(optionsDescriptor.ImplementationInstance);

        // Assert
        Assert.Equal(123, options.CommandTimeoutSeconds);
        Assert.True(options.StrictAutoMapping);
    }
}
