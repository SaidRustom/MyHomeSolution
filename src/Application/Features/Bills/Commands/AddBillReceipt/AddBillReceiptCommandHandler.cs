using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Bills.Commands.AddBillReceipt;

public sealed class AddBillReceiptCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService,
    IPublisher publisher)
    : IRequestHandler<AddBillReceiptCommand, string>
{
    private const string ContainerName = "receipts";

    public async Task<string> Handle(AddBillReceiptCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var bill = await dbContext.Bills
            .FirstOrDefaultAsync(b => b.Id == request.BillId && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Bill), request.BillId);

        var uniqueFileName = $"{bill.Id}/{Guid.CreateVersion7()}{Path.GetExtension(request.FileName)}";

        var receiptUrl = await fileStorageService.UploadAsync(
            ContainerName, uniqueFileName, request.Content, request.ContentType, cancellationToken);

        bill.ReceiptUrl = receiptUrl;
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BillReceiptAddedEvent(bill.Id, bill.Title, userId),
            cancellationToken);

        return receiptUrl;
    }
}
