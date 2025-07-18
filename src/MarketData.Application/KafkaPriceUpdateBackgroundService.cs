using Confluent.Kafka;
using MarketData.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MarketData.Application
{
    public class KafkaPriceUpdateBackgroundService : BackgroundService
    {
        private readonly ILogger<KafkaPriceUpdateBackgroundService> _logger;
        private readonly IConfiguration _config;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<PriceUpdate>> _history;
        private readonly int _movingAverageLength;
        private readonly decimal _spikeThresholdPercent;
        private readonly SemaphoreSlim _semaphore;

        public KafkaPriceUpdateBackgroundService(
            ILogger<KafkaPriceUpdateBackgroundService> logger,
            IConfiguration config,
            IOptions<MarketDataSettings> options)
        {
            _logger = logger;
            _config = config;
            _history = new ConcurrentDictionary<string, ConcurrentQueue<PriceUpdate>>();
            _movingAverageLength = options.Value.MovingAverageLength;
            _spikeThresholdPercent = options.Value.SpikeThresholdPercent;
            _semaphore = new SemaphoreSlim(Environment.ProcessorCount * 100);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
                GroupId = "price-update-consumer",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = true
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe("price-updates");

            int totalRequestsProcessed = 0;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(stoppingToken);
                        var update = JsonSerializer.Deserialize<PriceUpdate>(result.Message.Value);

                        if (update == null) continue;

                        await _semaphore.WaitAsync(stoppingToken);
                        _ = ProcessPriceUpdate(update, stoppingToken);

                        totalRequestsProcessed++;

                        if (stopwatch.ElapsedMilliseconds >= 1000)
                        {
                            _logger.LogInformation($"Requests Processed in Last Second: {totalRequestsProcessed}");
                            totalRequestsProcessed = 0;
                            stopwatch.Restart();
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError($"Kafka consume error: {ex.Error.Reason}");
                    }
                }
            }
            finally
            {
                consumer.Close();
            }
        }

        private Task ProcessPriceUpdate(PriceUpdate update, CancellationToken stoppingToken)
        {
            try
            {
                var queue = _history.GetOrAdd(update.Symbol, _ => new ConcurrentQueue<PriceUpdate>());
                queue.Enqueue(update);

                while (queue.Count > _movingAverageLength)
                    queue.TryDequeue(out _);

                var history = queue.ToArray();
                var oneSecAgo = update.Timestamp.AddSeconds(-1);
                var old = history.FirstOrDefault(p => p.Timestamp <= oneSecAgo);

                if (old != null)
                {
                    var change = Math.Abs((update.Price - old.Price) / old.Price * 100);
                    if (change > _spikeThresholdPercent)
                        _logger.LogWarning($"Spike Detected for {update.Symbol}: {change:F2}% at {update.Timestamp:HH:mm:ss.fff}");
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return Task.CompletedTask;
        }
    }
}
