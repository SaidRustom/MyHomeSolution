using MediatR;

namespace MyHomeSolution.Application.Features.Bills.Commands.MarkSplitAsPaid;

public sealed record MarkSplitAsPaidCommand(Guid BillId, Guid SplitId) : IRequest;
