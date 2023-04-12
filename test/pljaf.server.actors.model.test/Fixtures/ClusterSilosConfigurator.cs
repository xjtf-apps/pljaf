namespace pljaf.server.model.test;

public class ClusterSilosConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorageAsDefault();
        siloBuilder.AddMemoryGrainStorage(Constants.Stores.MediaStore);
    }
}
