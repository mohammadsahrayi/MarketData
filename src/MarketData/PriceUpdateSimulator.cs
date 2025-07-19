using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MarketData.Application.Abstractions;
using MarketData.Application;
using MarketData.Infrastructure.Services;
using FluentValidation;
using MarketData.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace MarketData
{
    public class PriceUpdateSimulator
    {
        public static Task Main(string[] args)
        {
            var host = Host
                .CreateDefaultBuilder(args)

                .ConfigureServices((context, services) =>
                {
                    _ = KfkaConfig.EnsureTopicExistsAsync("price-updates", context.Configuration, partitions: 12, replicationFactor: 1);
                    services.AddSingleton<IPriceUpdateProcessor, KafkaPriceQueue>();
                    services.AddHostedService<PriceUpdateGeneratorService>();

                    services.AddHostedService<KafkaPriceUpdateBackgroundService>();

                    services.AddLogging(config => config.AddConsole());
                    services.AddSingleton<IValidator<PriceUpdate>, PriceUpdateValidator>();

                    services.Configure<MarketDataSettings>(context.Configuration.GetSection("MarketDataSettings"));


                })
                .Build();

            return host.RunAsync();
        }
    }

}