using Azure;
using Azure.Data.Tables;

namespace voks.server.records
{
    public class GrainStoreTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public byte[] Data { get; set; }
    }
}
