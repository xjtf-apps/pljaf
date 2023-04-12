namespace pljaf.server.model.test;

[CollectionDefinition(ClusterCollection.Name)]
public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}