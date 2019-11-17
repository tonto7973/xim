namespace AzureServiceBusSample.Azure
{
    public class AzureServiceBusSettings
    {
        public string InConnectionString { get; set; }

        public string InQueueName { get; set; }

        public string OutConnectionString { get; set; }

        public string OutQueueName { get; set; }
    }
}