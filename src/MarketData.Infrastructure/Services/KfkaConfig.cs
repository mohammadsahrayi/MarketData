using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;

namespace MarketData.Infrastructure.Services
{
    public class KfkaConfig
    {
        public static async Task EnsureTopicExistsAsync(string topicName, IConfiguration config, int partitions = 12, short replicationFactor = 1)
        {
            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092"
            };

            using var adminClient = new AdminClientBuilder(adminConfig).Build();

            try
            {
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                if (metadata.Topics.Any(t => t.Topic == topicName))
                {
                    Console.WriteLine($"✔️ Topic '{topicName}' already exists.");
                    return;
                }

                await adminClient.CreateTopicsAsync(new TopicSpecification[]
                {
                    new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = partitions,
                        ReplicationFactor = replicationFactor,
                        Configs = new Dictionary<string, string>
                        {
                            { "compression.type", "lz4" },
                            { "retention.ms", "600000" }, // 10 minutes for fast-paced data
                            { "cleanup.policy", "delete" },
                            { "segment.bytes", "1073741824" } // 1GB segments
                        }
                    }
                });

                Console.WriteLine($"✅ Topic '{topicName}' created with {partitions} partitions.");
            }
            catch (CreateTopicsException ex)
            {
                if (ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
                    Console.WriteLine("⚠️ Topic already exists (race condition).");
                else
                    throw;
            }
        }
    }
}
