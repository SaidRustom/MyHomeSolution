# MyHomeSolution — Critical Pipelines

## 1. High-Level Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────────┐     ┌───────────────┐
│  BlazorUI   │────▶│   Api       │────▶│  Application    │────▶│ Infrastructure│
│  (WASM)     │ HTTP│  (ASP.NET)  │ Med │  (MediatR)      │ EF  │  (EF/SignalR) │
│             │     │  Controllers│ iatr│  Handlers/      │Core │  DbContext/   │
│             │◀────│  Middleware │◀────│  Behaviors/     │◀────│  Services     │
│             │ JSON│             │     │  Events          │     │               │
└─────────────┘     └─────────────┘     └─────────────────┘     └───────────────┘
                                                                        │
                                                                        ▼
                                                                ┌───────────────┐
                                                                │   Domain      │
                                                                │   Entities/   │
                                                                │   Enums/      │
                                                                │   Interfaces  │
                                                                └───────────────┘
```

---

## 2. HTTP Request Pipeline (ASP.NET Middleware)

Incoming HTTP requests traverse the ASP.NET middleware stack **in order** before
reaching any controller.

```
  HTTP Request
       │
       ▼
┌──────────────────────────────────┐
│  ExceptionHandlingMiddleware     │  Catches all exceptions and maps them:
│                                  │    ValidationException   → 400
│                                  │    NotFoundException     → 404
│                                  │    ForbiddenAccessEx     → 403
│                                  │    ConflictException     → 409
│                                  │    Unhandled             → 500
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Serilog Request Logging         │  Structured request/response log
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Response Compression            │  Gzip/Brotli for HTTPS
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  HTTPS Redirection               │
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Rate Limiter                    │  Fixed window: 60 req/min
│                                  │  Queue limit: 5
│                                  │  Reject → 429 Too Many Requests
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Authentication                  │  ASP.NET Identity (Bearer tokens)
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Authorization                   │  Policy-based authorization
└──────────────┬───────────────────┘
               ▼
┌──────────────────────────────────┐
│  Routing / Controller Action     │  Dispatches to controller
│                                  │  → sends MediatR command/query
└──────────────────────────────────┘
```

---

## 3. MediatR Behavior Pipeline (CQRS)

Every `IRequest` dispatched via MediatR passes through a **chain of
`IPipelineBehavior<,>` decorators** registered in `DependencyInjection.cs`.
They execute in registration order, wrapping the inner handler like
Russian nesting dolls.

```
  Controller sends IRequest via MediatR
       │
       ▼
┌──────────────────────────────────────────────────────────────────────┐
│ 1. UnhandledExceptionBehavior                                        │
│    • Wraps everything in try/catch                                   │
│    • Logs the exception with ILogger (request name + payload)        │
│    • Re-throws (lets ExceptionHandlingMiddleware produce HTTP error)  │
│                                                                      │
│    ┌────────────────────────────────────────────────────────────────┐ │
│    │ 2. AuthorizationBehavior                                      │ │
│    │    • Checks if request implements IRequireAuthorization        │ │
│    │    • Resolves current user via ICurrentUserService             │ │
│    │    • Checks SharePermission via IShareService.HasAccessAsync   │ │
│    │      ─ IRequireEditAccess  → SharePermission.Edit             │ │
│    │      ─ IRequireViewAccess  → SharePermission.View             │ │
│    │    • Throws ForbiddenAccessException on failure                │ │
│    │                                                                │ │
│    │    ┌──────────────────────────────────────────────────────────┐│ │
│    │    │ 3. ValidationBehavior                                    ││ │
│    │    │    • Collects all IValidator<TRequest> from DI           ││ │
│    │    │    • Runs FluentValidation rules in parallel             ││ │
│    │    │    • Throws ValidationException if any failures          ││ │
│    │    │                                                          ││ │
│    │    │    ┌────────────────────────────────────────────────────┐││ │
│    │    │    │ 4. LoggingBehavior                                 │││ │
│    │    │    │    • Logs "Handling {RequestName}" + payload       │││ │
│    │    │    │    • Calls next()                                  │││ │
│    │    │    │    • Logs "Handled  {RequestName}"                 │││ │
│    │    │    │                                                    │││ │
│    │    │    │    ┌──────────────────────────────────────────────┐│││ │
│    │    │    │    │ 5. Command/Query Handler                     ││││ │
│    │    │    │    │    (IRequestHandler<TRequest, TResponse>)    ││││ │
│    │    │    │    │    • Business logic                          ││││ │
│    │    │    │    │    • DbContext read/write                    ││││ │
│    │    │    │    │    • May publish INotification events        ││││ │
│    │    │    │    └──────────────────────────────────────────────┘│││ │
│    │    │    └────────────────────────────────────────────────────┘││ │
│    │    └──────────────────────────────────────────────────────────┘│ │
│    └────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 4. Persistence Pipeline (SaveChangesAsync)

`ApplicationDbContext.SaveChangesAsync` intercepts EF Core's change tracker
before flushing to SQL Server.

```
  handler calls SaveChangesAsync()
       │
       ▼
┌────────────────────────────────────────────┐
│ ProcessAuditableEntities()                 │
│                                            │
│  For each tracked IAuditableEntity:        │
│                                            │
│  ┌──── Added ─────────────────────────┐    │
│  │ • Set CreatedAt / CreatedBy        │    │
│  │ • Insert AuditLog (Create)         │    │
│  └────────────────────────────────────┘    │
│                                            │
│  ┌──── Modified ──────────────────────┐    │
│  │ • Set LastModifiedAt / By          │    │
│  │ • Diff changed properties          │    │
│  │ • Insert AuditLog (Update)         │    │
│  │   + AuditHistoryEntry per property │    │
│  └────────────────────────────────────┘    │
│                                            │
│  ┌──── Deleted ───────────────────────┐    │
│  │ • If ISoftDeletable:               │    │
│  │   ─ Convert to Modified state      │    │
│  │   ─ Set IsDeleted / DeletedAt / By │    │
│  │ • Insert AuditLog (Delete)         │    │
│  └────────────────────────────────────┘    │
│                                            │
│  base.SaveChangesAsync()  ──▶ SQL Server   │
└────────────────────────────────────────────┘
```

---

## 5. Domain Event / Notification Pipeline

Command handlers publish `INotification` domain events after persisting.
MediatR dispatches each event to **all** registered `INotificationHandler<T>`
implementations.

Each domain event typically has **two** handler types:

| Handler Kind | Responsibility |
|---|---|
| **Realtime handler** (e.g. `TaskCreatedEventHandler`) | Pushes real-time DTO via SignalR |
| **Notification handler** (e.g. `TaskCreatedNotificationHandler`) | Persists a `Notification` entity and sends a push via SignalR |

```
  Handler publishes INotification
       │
       ├──────────────────────────────┐
       ▼                              ▼
┌──────────────────────┐   ┌──────────────────────────────┐
│ Realtime EventHandler│   │ Notification EventHandler     │
│                      │   │                               │
│ • Build DTO          │   │ • Load related entity         │
│   (TaskNotification, │   │ • Create Notification entity  │
│    OccurrenceNotif., │   │ • SaveChangesAsync            │
│    etc.)             │   │ • Build UserPushNotification   │
│ • Call IRealtimeNoti │   │ • Call IRealtimeNotification   │
│   ficationService    │   │   Service                     │
└─────────┬────────────┘   └──────────────┬────────────────┘
          │                               │
          ▼                               ▼
┌──────────────────────────────────────────────────────────┐
│              SignalRNotificationService                   │
│                                                          │
│  ┌─────────────────────────────────────────────────┐     │
│  │ TaskHub         (/hubs/tasks)                   │     │
│  │  • SendTaskNotificationAsync → All clients      │     │
│  │  • SendOccurrenceNotificationAsync → Task group │     │
│  └─────────────────────────────────────────────────┘     │
│                                                          │
│  ┌─────────────────────────────────────────────────┐     │
│  │ NotificationHub  (/hubs/notifications)          │     │
│  │  • SendUserNotificationAsync → User group       │     │
│  └─────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────┘
          │                               │
          ▼                               ▼
     BlazorUI /                     BlazorUI /
     any SignalR                    any SignalR
     client                         client
```

### Domain Events Catalog

| Event | Trigger |
|---|---|
| `TaskCreatedEvent` | Task created |
| `TaskUpdatedEvent` | Task updated |
| `TaskDeletedEvent` | Task deleted |
| `OccurrenceCompletedEvent` | Occurrence completed |
| `OccurrenceSkippedEvent` | Occurrence skipped |
| `BillCreatedEvent` | Bill created |
| `BillUpdatedEvent` | Bill updated |
| `BillDeletedEvent` | Bill deleted |
| `BillSplitPaidEvent` | Bill split marked paid |
| `BillReceiptAddedEvent` | Receipt attached to bill |
| `ShoppingListCreatedEvent` | Shopping list created |
| `ShoppingListUpdatedEvent` | Shopping list updated |
| `ShoppingListDeletedEvent` | Shopping list deleted |
| `ShoppingItemCheckedEvent` | Shopping item toggled |
| `EntitySharedEvent` | Entity shared with user |
| `ShareRevokedEvent` | Share revoked |
| `NotificationCreatedEvent` | Notification created (push) |

---

## 6. Authorization / Sharing Pipeline

```
  IRequest implements IRequireAuthorization
       │
       ▼
  AuthorizationBehavior
       │
       ├─ Resolve userId from ICurrentUserService
       │
       ├─ Determine required permission:
       │    IRequireEditAccess  → Edit
       │    IRequireViewAccess  → View  (default)
       │
       ├─ Call IShareService.HasAccessAsync(
       │        resourceType, resourceId, userId, permission)
       │
       │       ShareService checks:
       │         1. Is the user the owner (CreatedBy)?  → ✅
       │         2. Does an EntityShare row exist
       │            with sufficient permission?         → ✅ / ❌
       │
       ├─ Access granted → next()
       └─ Access denied  → throw ForbiddenAccessException
                                    ↓
                           ExceptionHandlingMiddleware → 403
```

---

## 7. End-to-End Request Flow (Example: Create Task)

```
BlazorUI                     Api                        Application                    Infrastructure
   │                          │                              │                              │
   │  POST /api/tasks         │                              │                              │
   │─────────────────────────▶│                              │                              │
   │                          │  ExceptionHandling           │                              │
   │                          │  Serilog Logging             │                              │
   │                          │  ResponseCompression         │                              │
   │                          │  HTTPS Redirect              │                              │
   │                          │  Rate Limiter (60/min)       │                              │
   │                          │  Authentication              │                              │
   │                          │  Authorization               │                              │
   │                          │  ──▶ TasksController         │                              │
   │                          │      .CreateAsync()          │                              │
   │                          │         │                    │                              │
   │                          │         │  _mediator.Send()  │                              │
   │                          │         │───────────────────▶│                              │
   │                          │         │                    │  1. UnhandledExceptionBeh.   │
   │                          │         │                    │  2. AuthorizationBehavior     │
   │                          │         │                    │  3. ValidationBehavior        │
   │                          │         │                    │     (CreateTaskCommandValid.) │
   │                          │         │                    │  4. LoggingBehavior           │
   │                          │         │                    │  5. CreateTaskCommandHandler  │
   │                          │         │                    │         │                     │
   │                          │         │                    │         │  dbContext.Add()     │
   │                          │         │                    │         │  SaveChangesAsync()  │
   │                          │         │                    │         │────────────────────▶ │
   │                          │         │                    │         │                     │ ProcessAuditableEntities
   │                          │         │                    │         │                     │ base.SaveChangesAsync
   │                          │         │                    │         │◀────────────────────│
   │                          │         │                    │         │                     │
   │                          │         │                    │         │ _publisher.Publish   │
   │                          │         │                    │         │  (TaskCreatedEvent)  │
   │                          │         │                    │         │                     │
   │                          │         │                    │  ┌──────┴──────────────┐      │
   │                          │         │                    │  │TaskCreatedEvent     │      │
   │                          │         │                    │  │Handler (SignalR)    │─────▶│ TaskHub broadcast
   │                          │         │                    │  ├─────────────────────┤      │
   │                          │         │                    │  │TaskCreatedNotif.    │      │
   │                          │         │                    │  │Handler (persist +   │─────▶│ Save Notification
   │                          │         │                    │  │push)               │      │ + NotificationHub
   │                          │         │                    │  └─────────────────────┘      │
   │                          │         │                    │                              │
   │                          │         │◀───────────────────│  return TaskId               │
   │                          │◀────────│  201 Created       │                              │
   │◀─────────────────────────│  JSON   │                    │                              │
   │                          │         │                    │                              │
   │  ◀── SignalR push ──────────────────────────────────────────────────────────────────── │
   │   TaskNotification +     │         │                    │                              │
   │   UserPushNotification   │         │                    │                              │
```

---

## 8. Feature Domain Summary

| Domain | Commands | Queries | Events |
|---|---|---|---|
| **Tasks** | Create, Update, Delete | GetTasks, GetTaskById | Created, Updated, Deleted |
| **Occurrences** | Complete, Skip | GetOccurrencesByTask | Completed, Skipped |
| **Bills** | Create, Update, Delete, MarkSplitAsPaid, AddReceipt, CreateFromReceipt | GetBills, GetBillById, GetBillReceipt, GetUserBalances, GetSpendingSummary | Created, Updated, Deleted, SplitPaid, ReceiptAdded |
| **Shopping Lists** | Create, Update, Delete, AddItem, UpdateItem, RemoveItem, ToggleItem, AddItemFromBillItem, ProcessReceipt | GetShoppingLists, GetShoppingListById | Created, Updated, Deleted, ItemChecked |
| **Notifications** | Create, MarkAsRead, MarkAllAsRead, Delete | GetNotifications, GetNotificationById, GetUnreadCount | Created |
| **Users** | Create, Update, ChangePassword, ToggleActivation, AssignRole, RemoveRole | GetUsers, GetUserById | — |
| **Shares** | ShareEntity, UpdateSharePermission, RevokeShare | GetEntityShares | EntityShared, ShareRevoked |
