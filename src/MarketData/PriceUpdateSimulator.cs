using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MarketData.Application.Abstractions;
using MarketData.Application;
using MarketData.Infrastructure.Services;
using FluentValidation;
using MarketData.Domain.Entities;

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
                    services.AddSingleton<InMemoryPriceQueue>();

                    services.AddSingleton<IPriceUpdateProcessor>(provider =>
                        provider.GetRequiredService<InMemoryPriceQueue>());

                    services.AddHostedService<PriceUpdateBackgroundService>();
                    services.AddHostedService<PriceUpdateGeneratorService>();

                    services.AddLogging(config => config.AddConsole());
                    services.AddSingleton<IValidator<PriceUpdate>, PriceUpdateValidator>();

                    services.Configure<MarketDataSettings>(context.Configuration.GetSection("MarketDataSettings"));
                })
                .Build();

            return host.RunAsync();
        }
    }

}