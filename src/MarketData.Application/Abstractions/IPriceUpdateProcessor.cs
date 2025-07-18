using MarketData.Domain.Entities;

namespace MarketData.Application.Abstractions
{
    public interface IPriceUpdateProcessor
    {
        Task EnqueueAsync(PriceUpdate update);
    }
}
