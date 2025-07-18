using FluentValidation;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MarketData.Infrastructure.Services
{
    public class PriceUpdateGeneratorService : BackgroundService
    {
        private readonly IPriceUpdateProcessor _processor;
        private readonly ILogger<PriceUpdateGeneratorService> _logger;
        private readonly IValidator<PriceUpdate> _validator;
        private readonly Random _random = new();
        private readonly string[] _symbols = new[]
        {
        "fameli", "vabemellat", "shapna", "folad", "shsta"
        };

        public PriceUpdateGeneratorService(IPriceUpdateProcessor processor, ILogger<PriceUpdateGeneratorService> logger, IValidator<PriceUpdate> validator)
        {
            _processor = processor;
            _logger = logger;
            _validator = validator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMicroseconds(50);
            var batchSize = 10000;  

            int totalRequestsSent = 0; 
            var stopwatch = new Stopwatch(); 
            stopwatch.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                var tasks = new List<Task>();
                var now = DateTime.UtcNow;

                for (int i = 0; i < batchSize; i++)
                {
                    var ts = now.AddMilliseconds(-_random.Next(0, 1000)); 
                    var update = new PriceUpdate
                    {
                        Symbol = _symbols[_random.Next(_symbols.Length)],
                        Price = (decimal)(_random.NextDouble() * 50000 + 1000),
                        Timestamp = ts
                    };

                    var validationResult = _validator.Validate(update);
                    if (!validationResult.IsValid)
                    {

                        foreach (var failure in validationResult.Errors)
                        {
                            _logger.LogWarning($"Validation failed for {failure.PropertyName}: {failure.ErrorMessage}");
                        }
                        continue;
                    }

                    tasks.Add(_processor.EnqueueAsync(update));
                }

                await Task.WhenAll(tasks);
                totalRequestsSent += batchSize;


                if (stopwatch.ElapsedMilliseconds >= 1000)
                {
                    _logger.LogInformation($"Requests Sent in Last Second: {totalRequestsSent}");
                    totalRequestsSent = 0;
                    stopwatch.Restart();
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}
