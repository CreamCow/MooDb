namespace MooDb.Tests.Unit.Configuration;

public sealed class MooDbOptionsTests
{
    [Fact]
    public void Constructor_WhenCreated_UsesExpectedDefaults()
    {
        var options = new MooDbOptions();

        Assert.Equal(30, options.CommandTimeoutSeconds);
        Assert.False(options.StrictAutoMapping);
    }

    [Fact]
    public void Properties_WhenAssigned_RetainAssignedValues()
    {
        var options = new MooDbOptions
        {
            CommandTimeoutSeconds = 120,
            StrictAutoMapping = true
        };

        Assert.Equal(120, options.CommandTimeoutSeconds);
        Assert.True(options.StrictAutoMapping);
    }
}
