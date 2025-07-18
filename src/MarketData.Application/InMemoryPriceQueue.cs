using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;
using System.Threading.Channels;

namespace MarketData.Application
{
    public class InMemoryPriceQueue : IPriceUpdateProcessor
    {
        private readonly Channel<PriceUpdate> _channel;

        public InMemoryPriceQueue()
        {
            _channel = Channel.CreateUnbounded<PriceUpdate>(new UnboundedChannelOptions
            {
                SingleReader = true, 
                AllowSynchronousContinuations = false 
            });
        }
        public async Task EnqueueAsync(PriceUpdate update)
        {
            await _channel.Writer.WriteAsync(update);
        }
        public ChannelReader<PriceUpdate> Reader => _channel.Reader;
    }
}
