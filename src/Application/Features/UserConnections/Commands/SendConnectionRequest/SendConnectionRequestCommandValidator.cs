using FluentValidation;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.SendConnectionRequest;

public sealed class SendConnectionRequestCommandValidator : AbstractValidator<SendConnectionRequestCommand>
{
    public SendConnectionRequestCommandValidator()
    {
        RuleFor(x => x.AddresseeId)
            .NotEmpty().WithMessage("Addressee user id is required.");
    }
}
