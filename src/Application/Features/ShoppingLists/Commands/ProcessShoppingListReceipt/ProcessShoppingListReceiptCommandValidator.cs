using FluentValidation;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.ProcessShoppingListReceipt;

public sealed class ProcessShoppingListReceiptCommandValidator
    : AbstractValidator<ProcessShoppingListReceiptCommand>
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    public ProcessShoppingListReceiptCommandValidator()
    {
        RuleFor(x => x.ShoppingListId)
            .NotEmpty().WithMessage("Shopping list id is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(ct => AllowedContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}.");

        RuleFor(x => x.Content)
            .NotNull().WithMessage("File content is required.")
            .Must(s => s.CanRead).WithMessage("File stream is not readable.")
            .Must(s => s.Length <= MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.");

        RuleFor(x => x.Splits)
            .Must(splits =>
            {
                if (splits is null || splits.Count == 0)
                    return true;

                return splits.Select(s => s.UserId).Distinct().Count() == splits.Count;
            })
            .WithMessage("Duplicate user ids in splits are not allowed.")
            .Must(splits =>
            {
                if (splits is null || splits.Count == 0)
                    return true;

                var customPercentages = splits.Where(s => s.Percentage.HasValue).ToList();
                if (customPercentages.Count == 0)
                    return true;

                if (customPercentages.Count != splits.Count)
                    return false;

                return Math.Abs(customPercentages.Sum(s => s.Percentage!.Value) - 100m) < 0.01m;
            })
            .WithMessage("When custom percentages are specified, all splits must have a percentage and they must total 100%.");

        RuleForEach(x => x.Splits).ChildRules(split =>
        {
            split.RuleFor(s => s.UserId)
                .NotEmpty().WithMessage("Split user id is required.");

            split.RuleFor(s => s.Percentage)
                .GreaterThan(0).When(s => s.Percentage.HasValue)
                .WithMessage("Percentage must be greater than zero.")
                .LessThanOrEqualTo(100).When(s => s.Percentage.HasValue)
                .WithMessage("Percentage must not exceed 100.");
        });
    }
}
