using Confluent.Kafka;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MarketData.Application
{
    public class KafkaPriceQueue : IPriceUpdateProcessor, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topic = "price-updates";

        public KafkaPriceQueue(IConfiguration config)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092",
                LingerMs = 5, // small batching
                BatchSize = 32768, // 32KB
                QueueBufferingMaxMessages = 1000000,
                CompressionType = CompressionType.Lz4,
                Acks = Acks.None,
                EnableIdempotence = false
            };
            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public Task EnqueueAsync(PriceUpdate update)
        {
            var json = JsonSerializer.Serialize(update);
            _producer.Produce(_topic, new Message<string, string>
            {
                Key = update.Symbol,
                Value = json
            }, null); // no delivery report for speed
            return Task.CompletedTask;
        }

        public void Dispose() => _producer?.Flush(TimeSpan.FromSeconds(3));
    }
}
