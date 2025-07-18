namespace MarketData.Domain.Entities
{
    public class PriceUpdate
    {
        public string Symbol { get; set; } = default!;
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
