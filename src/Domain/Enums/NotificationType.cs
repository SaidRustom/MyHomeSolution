namespace MyHomeSolution.Domain.Enums;

public enum NotificationType
{
    General = 0,
    TaskAssigned = 1,
    TaskUpdated = 2,
    TaskDeleted = 3,
    TaskDueSoon = 4,
    OccurrenceCompleted = 5,
    OccurrenceSkipped = 6,
    ShareReceived = 7,
    ShareRevoked = 8,
    Mention = 9,
    BillCreated = 10,
    BillUpdated = 11,
    BillDeleted = 12,
    BillSplitPaid = 13,
    BillReceiptAdded = 14,
    ShoppingListCreated = 15,
    ShoppingListUpdated = 16,
    ShoppingListDeleted = 17,
    ShoppingItemChecked = 18,
    ConnectionRequestReceived = 19,
    ConnectionRequestAccepted = 20,
    OccurrenceOverdue = 21,
    OccurrenceStarted = 22,
    OccurrenceRescheduled = 23
}
