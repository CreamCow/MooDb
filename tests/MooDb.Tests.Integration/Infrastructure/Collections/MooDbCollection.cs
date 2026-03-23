using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Infrastructure.Collections;

[CollectionDefinition("MooDb")]
public sealed class MooDbCollection : ICollectionFixture<MooDbFixture>
{
}