namespace voks.server.model.test;

public class ClusterFixture : IDisposable
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<ClusterSilosConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }
}
