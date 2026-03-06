using FluentValidation;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.UpdateShoppingList;

public sealed class UpdateShoppingListCommandValidator : AbstractValidator<UpdateShoppingListCommand>
{
    public UpdateShoppingListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Shopping list id is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(256).WithMessage("Title must not exceed 256 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid shopping list category.");
    }
}
