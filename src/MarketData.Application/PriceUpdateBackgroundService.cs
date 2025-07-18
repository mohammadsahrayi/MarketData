using MarketData.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace MarketData.Application
{
    public class PriceUpdateBackgroundService : BackgroundService
    {
        private readonly ChannelReader<PriceUpdate> _reader;
        private readonly ILogger<PriceUpdateBackgroundService> _logger;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<PriceUpdate>> _history;
        private readonly int _movingAverageLength;
        private readonly decimal _spikeThresholdPercent;
        private readonly SemaphoreSlim _semaphore;

        public PriceUpdateBackgroundService(
            InMemoryPriceQueue queue,
            ILogger<PriceUpdateBackgroundService> logger,
            IOptions<MarketDataSettings> options)
        {
            _reader = queue.Reader;
            _logger = logger;
            _history = new ConcurrentDictionary<string, ConcurrentQueue<PriceUpdate>>();
            _movingAverageLength = options.Value.MovingAverageLength;
            _spikeThresholdPercent = options.Value.SpikeThresholdPercent;

            _semaphore = new SemaphoreSlim(Environment.ProcessorCount * 100, Environment.ProcessorCount * 100);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var processingTasks = new List<Task>();
            int totalRequestsProcessed = 0;  
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await foreach (var update in _reader.ReadAllAsync(stoppingToken))
            {
                await _semaphore.WaitAsync(stoppingToken);

                processingTasks.Add(ProcessPriceUpdate(update, stoppingToken));

                totalRequestsProcessed++;

                if (stopwatch.ElapsedMilliseconds >= 1000)
                {
                    _logger.LogInformation($"Requests Processed in Last Second: {totalRequestsProcessed}");
                    totalRequestsProcessed = 0;
                    stopwatch.Restart();
                }

                if (processingTasks.Count > Environment.ProcessorCount * 100)
                {
                    await Task.WhenAny(processingTasks);
                    processingTasks.RemoveAll(task => task.IsCompleted);
                }
            }
            await Task.WhenAll(processingTasks);
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
