using FluentValidation;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItemFromBillItem;

public sealed class AddShoppingItemFromBillItemCommandValidator
    : AbstractValidator<AddShoppingItemFromBillItemCommand>
{
    public AddShoppingItemFromBillItemCommandValidator()
    {
        RuleFor(x => x.ShoppingListId)
            .NotEmpty().WithMessage("Shopping list id is required.");

        RuleFor(x => x.BillItemId)
            .NotEmpty().WithMessage("Bill item id is required.");

        RuleFor(x => x.QuantityOverride)
            .GreaterThan(0)
            .When(x => x.QuantityOverride.HasValue)
            .WithMessage("Quantity override must be greater than zero.");

        RuleFor(x => x.UnitOverride)
            .MaximumLength(50)
            .When(x => x.UnitOverride is not null)
            .WithMessage("Unit override must not exceed 50 characters.");
    }
}
