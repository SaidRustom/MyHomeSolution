using FluentValidation;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.UpdateShoppingItem;

public sealed class UpdateShoppingItemCommandValidator : AbstractValidator<UpdateShoppingItemCommand>
{
    public UpdateShoppingItemCommandValidator()
    {
        RuleFor(x => x.ShoppingListId)
            .NotEmpty().WithMessage("Shopping list id is required.");

        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Item id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(500).WithMessage("Item name must not exceed 500 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.Unit)
            .MaximumLength(50).WithMessage("Unit must not exceed 50 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must not be negative.");
    }
}
