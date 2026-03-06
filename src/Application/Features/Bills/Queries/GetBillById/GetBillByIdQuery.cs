using MediatR;
using MyHomeSolution.Application.Features.Bills.Common;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetBillById;

public sealed record GetBillByIdQuery(Guid Id) : IRequest<BillDetailDto>;
