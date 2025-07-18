using FluentValidation;
using MarketData.Domain.Entities;

namespace MarketData.Application
{
    public class PriceUpdateValidator : AbstractValidator<PriceUpdate>
    {
        public PriceUpdateValidator()
        {
            RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Price).GreaterThan(0);
        }
    }
}
