using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetBillReceipt;

public sealed class GetBillReceiptQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService)
    : IRequestHandler<GetBillReceiptQuery, BillReceiptResult?>
{
    private const string ContainerName = "receipts";

    public async Task<BillReceiptResult?> Handle(GetBillReceiptQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var bill = await dbContext.Bills
            .AsNoTracking()
            .Include(b => b.Splits)
            .Where(b => !b.IsDeleted)
            .FirstOrDefaultAsync(b => b.Id == request.BillId, cancellationToken)
            ?? throw new NotFoundException(nameof(Bill), request.BillId);

        if (bill.CreatedBy != userId && !bill.Splits.Any(s => s.UserId == userId))
            throw new ForbiddenAccessException();

        if (string.IsNullOrEmpty(bill.ReceiptUrl))
            return null;

        // ReceiptUrl is stored as "/{containerName}/{fileName}" — strip the leading container segment
        var fileName = bill.ReceiptUrl
            .TrimStart('/')
            .Substring(ContainerName.Length + 1);

        var result = await fileStorageService.DownloadAsync(ContainerName, fileName, cancellationToken);
        if (result is null)
            return null;

        var (content, contentType) = result.Value;
        var downloadName = $"receipt-{bill.Id}{Path.GetExtension(fileName)}";

        return new BillReceiptResult(content, contentType, downloadName);
    }
}
