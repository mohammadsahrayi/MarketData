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
                BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092"
            };
            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task EnqueueAsync(PriceUpdate update)
        {
            var json = JsonSerializer.Serialize(update);
            await _producer.ProduceAsync(_topic, new Message<string, string>
            {
                Key = update.Symbol,
                Value = json
            });
        }

        public void Dispose() => _producer?.Dispose();
    }
}
