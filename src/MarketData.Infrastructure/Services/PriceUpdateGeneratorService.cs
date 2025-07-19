using FluentValidation;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Channels;

namespace MarketData.Infrastructure.Services
{
    public class PriceUpdateGeneratorService : BackgroundService
    {
        private readonly Channel<PriceUpdate> _channel;
        private readonly ILogger<PriceUpdateGeneratorService> _logger;
        private readonly IValidator<PriceUpdate> _validator;
        private readonly Random _random = new();
        private readonly string[] _symbols = { "fameli", "vabemellat", "shapna", "folad", "shsta" };

        public PriceUpdateGeneratorService(
            IPriceUpdateProcessor processor,
            ILogger<PriceUpdateGeneratorService> logger,
            IValidator<PriceUpdate> validator)
        {
            _channel = Channel.CreateUnbounded<PriceUpdate>(new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            });

            _logger = logger;
            _validator = validator;

            // consumer task
            _ = Task.Run(async () =>
            {
                await foreach (var item in _channel.Reader.ReadAllAsync())
                {
                    await processor.EnqueueAsync(item);
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopwatch = Stopwatch.StartNew();
            int total = 0;

            _ = Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        for (int i = 0; i < 10000; i++)
                        {
                            var update = new PriceUpdate
                            {
                                Symbol = _symbols[_random.Next(_symbols.Length)],
                                Price = (decimal)(_random.NextDouble() * 50000 + 1000),
                                Timestamp = DateTime.UtcNow.AddMilliseconds(-_random.Next(0, 1000))
                            };
              
                            if (_validator.Validate(update).IsValid)
                            {
                                await _channel.Writer.WriteAsync(update, stoppingToken);
                                total++;
                            }
                        }
              
                        if (stopwatch.ElapsedMilliseconds >= 1000)
                        {
                            _logger.LogInformation($"Generated/Sent: {total} updates/sec");
                            total = 0;
                            stopwatch.Restart();
                        }
              
                    }
                }, stoppingToken);
              }
    }
}
