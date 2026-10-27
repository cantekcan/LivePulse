namespace LivePulse.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<LivePulseApiFactory>
{
    public const string Name = "api";
}
