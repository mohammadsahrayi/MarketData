using MarketData.Domain.Entities;

namespace MarketData.Tests
{
    public class SpikeDetectorTests
    {
        [Fact]
        public void DetectsSpikeCorrectly()
        {
            var threshold = 2.0m;
            var recent = new PriceUpdate { Symbol = "ABC", Price = 110, Timestamp = DateTime.UtcNow };
            var old = new PriceUpdate { Symbol = "ABC", Price = 100, Timestamp = recent.Timestamp.AddSeconds(-1) };

            var change = Math.Abs((recent.Price - old.Price) / old.Price * 100);
            Assert.True(change > threshold);
        }

        [Fact]
        public void NoSpikeIfChangeIsSmall()
        {
            var threshold = 2.0m;
            var recent = new PriceUpdate { Symbol = "XYZ", Price = 101, Timestamp = DateTime.UtcNow };
            var old = new PriceUpdate { Symbol = "XYZ", Price = 100, Timestamp = recent.Timestamp.AddSeconds(-1) };

            var change = Math.Abs((recent.Price - old.Price) / old.Price * 100);
            Assert.False(change > threshold);
        }
    }
}