using MediatR;

namespace MyHomeSolution.Application.Features.Users.Commands.DeleteAccount;

public sealed record DeleteAccountCommand : IRequest<DeleteAccountResult>;

public sealed record DeleteAccountResult(string? Email, string? UserName);
