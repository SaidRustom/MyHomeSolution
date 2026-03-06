# MyHomeSolution — Complete API Reference

> **Version:** 1.0  
> **Last Updated:** 2025-07-12  
> **Base URL:** `https://<your-host>`  
> **Authentication:** ASP.NET Identity with Bearer tokens (see [Authentication](#authentication))  
> **Content-Type:** `application/json` unless stated otherwise

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Authentication](#authentication)
3. [Common Conventions](#common-conventions)
   - [Pagination](#pagination)
   - [Error Responses (Problem Details)](#error-responses-problem-details)
   - [ID Format](#id-format)
   - [Route / Body ID Matching](#route--body-id-matching)
   - [Soft Deletes](#soft-deletes)
   - [Rate Limiting](#rate-limiting)
4. **API Endpoints**
   - [Users API](#users-api)
   - [Tasks API](#tasks-api)
   - [Occurrences API](#occurrences-api)
   - [Bills API](#bills-api)
   - [Shopping Lists API](#shopping-lists-api)
   - [Shares API](#shares-api)
   - [Notifications API](#notifications-api)
5. [Enumerations Reference](#enumerations-reference)
6. [Real-Time (SignalR)](#real-time-signalr)
7. [File Uploads](#file-uploads)
8. [Authorization & Sharing Model](#authorization--sharing-model)
9. [Cross-Feature Workflows](#cross-feature-workflows)

---

## Quick Start

```
1. Register / Login    →  POST /register  or  POST /login
2. Get your profile    →  GET  /api/Users/me
3. Create a task       →  POST /api/Tasks
4. Create a bill       →  POST /api/Bills
5. Create a shopping list → POST /api/ShoppingLists
6. Share with a user   →  POST /api/Shares
7. Connect SignalR     →  /hubs/notifications, /hubs/tasks
```

All requests (except auth endpoints and `/health`) require a valid `Authorization: Bearer <token>` header.

---

## Authentication

The API uses **ASP.NET Identity** endpoints mapped at the root level. These are the standard Identity API endpoints:

| Endpoint | Method | Description |
|---|---|---|
| `/register` | `POST` | Register a new user account |
| `/login` | `POST` | Sign in and receive a Bearer token |
| `/refresh` | `POST` | Refresh an expired access token |
| `/confirmEmail` | `GET` | Confirm email address |
| `/forgotPassword` | `POST` | Initiate password reset |
| `/resetPassword` | `POST` | Complete password reset |
| `/manage/info` | `GET` | Get account info |
| `/manage/info` | `POST` | Update account info |
| `/manage/2fa` | `POST` | Manage two-factor authentication |

### Login Example

```http
POST /login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "YourPassword123!"
}
```

**Response `200 OK`:**

```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 3600,
  "refreshToken": "CfDJ8..."
}
```

Use the `accessToken` in all subsequent requests:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

## Common Conventions

### Pagination

All list endpoints return a paginated envelope:

```json
{
  "items": [ /* T[] */ ],
  "pageNumber": 1,
  "totalPages": 5,
  "totalCount": 98,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

| Field | Type | Description |
|---|---|---|
| `items` | `T[]` | Array of results for the current page |
| `pageNumber` | `int` | Current page (1-based) |
| `totalPages` | `int` | Total number of pages |
| `totalCount` | `int` | Total number of records matching the query |
| `hasPreviousPage` | `bool` | `true` if `pageNumber > 1` |
| `hasNextPage` | `bool` | `true` if `pageNumber < totalPages` |

**Standard pagination parameters** (supported on all list endpoints):

| Parameter | Type | Default |
|---|---|---|
| `pageNumber` | `int` | `1` |
| `pageSize` | `int` | `20` |

### Error Responses (Problem Details)

All errors follow the **RFC 7807 Problem Details** format:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation Error",
  "status": 400,
  "detail": "Title is required.",
  "traceId": "00-abc123def456...",
  "errors": {
    "Title": ["Title is required."],
    "Amount": ["Amount must be greater than zero."]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `type` | `string` | URL identifying the error type |
| `title` | `string` | Human-readable error category |
| `status` | `int` | HTTP status code |
| `detail` | `string` | Specific error message |
| `traceId` | `string` | Correlation ID for debugging — include this in bug reports |
| `errors` | `object?` | Field-level validation errors (only on `400` responses) |

### HTTP Status Codes Used Across the API

| Code | Meaning | Typical Cause |
|---|---|---|
| `200` | OK | Successful read or action |
| `201` | Created | Resource created — check the `Location` header |
| `204` | No Content | Update or delete succeeded |
| `400` | Bad Request | Validation failure, ID mismatch, or malformed input |
| `401` | Unauthorized | Missing or invalid Bearer token |
| `403` | Forbidden | Authenticated but no permission to access the resource |
| `404` | Not Found | Entity doesn't exist or was soft-deleted |
| `409` | Conflict | Duplicate resource (e.g., duplicate item name) |
| `429` | Too Many Requests | Rate limit exceeded (see [Rate Limiting](#rate-limiting)) |
| `499` | Client Closed Request | Client disconnected before the server finished |
| `500` | Internal Server Error | Unhandled server error — report the `traceId` |

### ID Format

All entity IDs are **GUIDs (UUID v7)**. They are time-sortable, so ordering by ID descending approximates "newest first."

Example: `01968b5a-0000-7000-0000-000000000001`

User IDs (from ASP.NET Identity) are **strings** (also GUIDs internally, but always transmitted as strings).

### Route / Body ID Matching

`PUT` and `POST` endpoints that accept both a route ID and a body ID **will return `400 Bad Request`** if they don't match. Always set both to the same value.

```
PUT /api/Tasks/01968b5a-0001-...
{
  "id": "01968b5a-0001-...",   ← MUST match the route
  ...
}
```

### Soft Deletes

Tasks, Bills, and Shopping Lists use soft deletes. After deletion:
- The entity returns `404` on subsequent requests
- It no longer appears in list queries
- There is no "undelete" endpoint

### Rate Limiting

The API enforces a **fixed-window rate limiter**:

| Setting | Value |
|---|---|
| Window | 1 minute |
| Max Requests | 60 per window |
| Queue Limit | 5 overflow requests |
| Queue Order | Oldest first |
| Exceeded Response | `429 Too Many Requests` |

---

## Users API

> **Base URL:** `/api/Users`  
> **Authentication:** Required. Admin endpoints require the `Administrator` role.

### Roles

| Role | Description |
|---|---|
| `Administrator` | Full access to all user management endpoints |
| `Member` | Standard household member (default) |

---

### 1. List Users *(Admin)*

```
GET /api/Users
```

**Authorization:** `Administrator` role required.

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `searchTerm` | `string?` | — | Search in name and email |
| `isActive` | `bool?` | — | Filter by active status |
| `pageNumber` | `int` | `1` | Page number |
| `pageSize` | `int` | `20` | Items per page |

#### Response `200 OK` — `PaginatedList<UserDto>`

```json
{
  "items": [
    {
      "id": "abc-123",
      "email": "john@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "fullName": "John Doe",
      "isActive": true,
      "createdAt": "2025-01-15T08:00:00+00:00"
    }
  ],
  "pageNumber": 1,
  "totalPages": 1,
  "totalCount": 3,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

---

### 2. Get User by ID *(Admin)*

```
GET /api/Users/{id}
```

**Authorization:** `Administrator` role required.

#### Response `200 OK` — `UserDetailDto`

```json
{
  "id": "abc-123",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "fullName": "John Doe",
  "avatarUrl": "https://example.com/avatar.jpg",
  "isActive": true,
  "emailConfirmed": true,
  "createdAt": "2025-01-15T08:00:00+00:00",
  "lastLoginAt": "2025-07-10T14:00:00+00:00",
  "roles": ["Member"]
}
```

---

### 3. Create User *(Admin)*

```
POST /api/Users
```

**Authorization:** `Administrator` role required.

#### Request Body

```json
{
  "email": "jane@example.com",
  "password": "SecurePass123!",
  "firstName": "Jane",
  "lastName": "Smith"
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `email` | `string` | ✅ | Valid email address |
| `password` | `string` | ✅ | Min 8 characters |
| `firstName` | `string` | ✅ | Max 100 chars |
| `lastName` | `string` | ✅ | Max 100 chars |

#### Response `201 Created`

Returns the new user's string ID.

---

### 4. Update User *(Admin)*

```
PUT /api/Users/{id}
```

**Authorization:** `Administrator` role required.

#### Request Body

```json
{
  "userId": "abc-123",
  "firstName": "Jane",
  "lastName": "Smith-Updated",
  "email": "jane.updated@example.com",
  "avatarUrl": "https://example.com/new-avatar.jpg"
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `userId` | `string` | ✅ | Must match route `{id}` |
| `firstName` | `string` | ✅ | Max 100 chars |
| `lastName` | `string` | ✅ | Max 100 chars |
| `email` | `string` | ✅ | Valid email address |
| `avatarUrl` | `string?` | — | Valid absolute URL, max 2048 chars |

#### Response `204 No Content`

---

### 5. Activate User *(Admin)*

```
POST /api/Users/{id}/activate
```

#### Response `204 No Content`

---

### 6. Deactivate User *(Admin)*

```
POST /api/Users/{id}/deactivate
```

#### Response `204 No Content`

---

### 7. Assign Role *(Admin)*

```
POST /api/Users/{id}/roles
```

#### Request Body

```json
{
  "userId": "abc-123",
  "role": "Administrator"
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `userId` | `string` | ✅ | Must match route `{id}` |
| `role` | `string` | ✅ | Must be `Administrator` or `Member` |

#### Response `204 No Content`

---

### 8. Remove Role *(Admin)*

```
DELETE /api/Users/{id}/roles/{roleName}
```

#### Response `204 No Content`

---

### 9. Get Current User Profile *(Self-Service)*

```
GET /api/Users/me
```

Returns `UserDetailDto` for the authenticated user.

#### Response `200 OK`

Same shape as [Get User by ID](#2-get-user-by-id-admin).

---

### 10. Update Current User Profile *(Self-Service)*

```
PUT /api/Users/me
```

#### Request Body

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.new@example.com",
  "avatarUrl": "https://example.com/avatar.jpg"
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `firstName` | `string` | ✅ | Max 100 chars |
| `lastName` | `string` | ✅ | Max 100 chars |
| `email` | `string` | ✅ | Valid email |
| `avatarUrl` | `string?` | — | Valid absolute URL, max 2048 chars |

#### Response `204 No Content`

---

### 11. Change Password *(Self-Service)*

```
POST /api/Users/me/change-password
```

#### Request Body

```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewSecurePass456!"
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `currentPassword` | `string` | ✅ | Must match existing password |
| `newPassword` | `string` | ✅ | Min 8 chars, must differ from current |

#### Response `204 No Content`

---

### Data Models — Users

#### UserDto

| Field | Type | Description |
|---|---|---|
| `id` | `string` | User ID |
| `email` | `string` | Email address |
| `firstName` | `string` | First name |
| `lastName` | `string` | Last name |
| `fullName` | `string` | Computed: `firstName + " " + lastName` |
| `isActive` | `bool` | Whether the account is active |
| `createdAt` | `datetimeoffset` | Account creation timestamp |

#### UserDetailDto

Extends `UserDto` with:

| Field | Type | Description |
|---|---|---|
| `avatarUrl` | `string?` | Profile picture URL |
| `emailConfirmed` | `bool` | Whether email has been confirmed |
| `lastLoginAt` | `datetimeoffset?` | Last login timestamp |
| `roles` | `string[]` | Assigned roles (`Administrator`, `Member`) |

---

## Tasks API

> **Base URL:** `/api/Tasks`  
> **Authentication:** Required (Bearer token)  
> **Sharing:** Tasks can be shared via the [Shares API](#shares-api). Shared users with `Edit` permission can update/delete.

### 1. List Tasks

```
GET /api/Tasks
```

Returns a paginated list of tasks owned by or shared with the current user.

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `pageNumber` | `int` | `1` | Page number |
| `pageSize` | `int` | `20` | Items per page |
| `category` | `int?` | — | Filter by `TaskCategory` |
| `priority` | `int?` | — | Filter by `TaskPriority` |
| `isRecurring` | `bool?` | — | Filter recurring vs one-time |
| `assignedToUserId` | `string?` | — | Filter by assigned user |
| `searchTerm` | `string?` | — | Search in title and description |
| `fromDate` | `DateOnly?` | — | Filter tasks with due date ≥ this value (`yyyy-MM-dd`) |
| `toDate` | `DateOnly?` | — | Filter tasks with due date ≤ this value (`yyyy-MM-dd`) |
| `notCompletedOnly` | `bool?` | — | Show only tasks that are not fully completed |

#### Response `200 OK` — `PaginatedList<TaskBriefDto>`

```json
{
  "items": [
    {
      "id": "01968b5a-...",
      "title": "Clean kitchen",
      "priority": 1,
      "category": 1,
      "isRecurring": true,
      "isActive": true,
      "nextDueDate": "2025-07-15",
      "assignedToUserId": "user-abc",
      "estimatedDurationMinutes": 30
    }
  ],
  "pageNumber": 1,
  "totalPages": 2,
  "totalCount": 25,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

### 2. Get Task by ID

```
GET /api/Tasks/{id}
```

Returns full details including recurrence pattern and occurrences.

#### Response `200 OK` — `TaskDetailDto`

```json
{
  "id": "01968b5a-...",
  "title": "Clean kitchen",
  "description": "Deep clean every surface",
  "priority": 1,
  "category": 1,
  "estimatedDurationMinutes": 30,
  "isRecurring": true,
  "isActive": true,
  "dueDate": null,
  "assignedToUserId": null,
  "createdAt": "2025-06-01T10:00:00+00:00",
  "recurrencePattern": {
    "id": "01968b5a-...",
    "type": 1,
    "interval": 1,
    "startDate": "2025-06-01",
    "endDate": null,
    "assigneeUserIds": ["user-abc", "user-def"]
  },
  "occurrences": [
    {
      "id": "01968b5a-...",
      "dueDate": "2025-07-08",
      "status": 2,
      "assignedToUserId": "user-abc",
      "completedAt": "2025-07-08T14:00:00+00:00",
      "notes": "All done"
    }
  ]
}
```

---

### 3. Create Task

```
POST /api/Tasks
```

#### Request Body

```json
{
  "title": "Clean kitchen",
  "description": "Deep clean every surface",
  "priority": 1,
  "category": 1,
  "estimatedDurationMinutes": 30,
  "isRecurring": true,
  "recurrenceType": 1,
  "interval": 1,
  "recurrenceStartDate": "2025-06-01",
  "recurrenceEndDate": null,
  "assigneeUserIds": ["user-abc", "user-def"]
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `title` | `string` | ✅ | Max 200 chars |
| `description` | `string?` | — | Max 2000 chars |
| `priority` | `int` | ✅ | Valid `TaskPriority` value |
| `category` | `int` | ✅ | Valid `TaskCategory` value |
| `estimatedDurationMinutes` | `int?` | — | Must be > 0 if provided |
| `isRecurring` | `bool` | ✅ | Determines which additional fields are required |
| `dueDate` | `string?` | **✅ if not recurring** | `yyyy-MM-dd` format |
| `assignedToUserId` | `string?` | — | User ID for one-time task assignment |
| `recurrenceType` | `int?` | **✅ if recurring** | Valid `RecurrenceType` value |
| `interval` | `int?` | **✅ if recurring** | Must be > 0 |
| `recurrenceStartDate` | `string?` | **✅ if recurring** | `yyyy-MM-dd` format |
| `recurrenceEndDate` | `string?` | — | `yyyy-MM-dd`, must be after start date |
| `assigneeUserIds` | `string[]?` | **✅ if recurring** | At least one assignee |

#### Response `201 Created`

```
Location: /api/Tasks/{id}
```

Returns the new task GUID.

---

### 4. Update Task

```
PUT /api/Tasks/{id}
```

**Authorization:** Requires edit access (owner or shared with `Edit` permission).

#### Request Body

```json
{
  "id": "01968b5a-...",
  "title": "Clean kitchen (updated)",
  "description": "Updated instructions",
  "priority": 2,
  "category": 1,
  "estimatedDurationMinutes": 45,
  "isActive": true,
  "dueDate": "2025-07-20",
  "assignedToUserId": "user-abc"
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | `guid` | ✅ | Must match route `{id}` |
| `title` | `string` | ✅ | Max 200 chars |
| `description` | `string?` | — | Max 2000 chars |
| `priority` | `int` | ✅ | Valid `TaskPriority` |
| `category` | `int` | ✅ | Valid `TaskCategory` |
| `estimatedDurationMinutes` | `int?` | — | Must be > 0 if provided |
| `isActive` | `bool` | ✅ | Whether the task is active |
| `dueDate` | `string?` | — | `yyyy-MM-dd` |
| `assignedToUserId` | `string?` | — | User ID |

#### Response `204 No Content`

---

### 5. Delete Task

```
DELETE /api/Tasks/{id}
```

Soft-deletes the task and all its occurrences.

#### Response `204 No Content`

---

### Data Models — Tasks

#### TaskBriefDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Task ID |
| `title` | `string` | Task title |
| `priority` | `int` | `TaskPriority` enum value |
| `category` | `int` | `TaskCategory` enum value |
| `isRecurring` | `bool` | Whether task has a recurrence pattern |
| `isActive` | `bool` | Whether the task is active |
| `nextDueDate` | `string?` | Next upcoming due date (`yyyy-MM-dd`) |
| `assignedToUserId` | `string?` | Currently assigned user |
| `estimatedDurationMinutes` | `int?` | Estimated completion time |

#### TaskDetailDto

All fields from `TaskBriefDto` plus:

| Field | Type | Description |
|---|---|---|
| `description` | `string?` | Task description |
| `dueDate` | `string?` | Due date for non-recurring tasks |
| `createdAt` | `datetimeoffset` | Creation timestamp |
| `recurrencePattern` | `RecurrencePatternDto?` | Recurrence configuration (if recurring) |
| `occurrences` | `OccurrenceDto[]` | Task occurrences |

#### RecurrencePatternDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Pattern ID |
| `type` | `int` | `RecurrenceType` enum value |
| `interval` | `int` | Recurrence interval (e.g., every 2 weeks → `type=1, interval=2`) |
| `startDate` | `string` | Pattern start date (`yyyy-MM-dd`) |
| `endDate` | `string?` | Pattern end date (null = indefinite) |
| `assigneeUserIds` | `string[]` | User IDs that rotate through occurrences |

#### OccurrenceDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Occurrence ID |
| `dueDate` | `string` | When this occurrence is due (`yyyy-MM-dd`) |
| `status` | `int` | `OccurrenceStatus` enum value |
| `assignedToUserId` | `string?` | Who is assigned to this specific occurrence |
| `completedAt` | `datetimeoffset?` | When the occurrence was completed |
| `notes` | `string?` | Notes added on completion or skip |

---

## Occurrences API

> **Base URL:** `/api/Occurrences`  
> **Authentication:** Required (Bearer token)

Occurrences represent individual instances of a task (especially for recurring tasks).

### 1. Get Occurrences by Task

```
GET /api/Occurrences/by-task/{taskId}
```

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `status` | `int?` | — | Filter by `OccurrenceStatus` |
| `pageNumber` | `int` | `1` | Page number |
| `pageSize` | `int` | `20` | Items per page |

#### Response `200 OK` — `PaginatedList<OccurrenceDto>`

---

### 2. Complete Occurrence

```
POST /api/Occurrences/{id}/complete
```

Marks an occurrence as completed.

#### Request Body *(optional)*

```json
{
  "notes": "Finished cleaning the kitchen"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `notes` | `string?` | — | Completion notes |

#### Response `204 No Content`

---

### 3. Skip Occurrence

```
POST /api/Occurrences/{id}/skip
```

Marks an occurrence as skipped.

#### Request Body *(optional)*

```json
{
  "notes": "Away on vacation"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `notes` | `string?` | — | Skip reason |

#### Response `204 No Content`

---

## Bills API

> **Base URL:** `/api/Bills`  
> **Authentication:** Required (Bearer token)  
> **Sharing:** Bills can be shared via the [Shares API](#shares-api).

### 1. List Bills

```
GET /api/Bills
```

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `pageNumber` | `int` | `1` | Page number |
| `pageSize` | `int` | `20` | Items per page |
| `category` | `int?` | — | Filter by `BillCategory` |
| `paidByUserId` | `string?` | — | Filter by who paid |
| `searchTerm` | `string?` | — | Search in title and description |
| `fromDate` | `datetimeoffset?` | — | Bills on or after this date |
| `toDate` | `datetimeoffset?` | — | Bills on or before this date |

#### Response `200 OK` — `PaginatedList<BillBriefDto>`

```json
{
  "items": [
    {
      "id": "01968b5a-...",
      "title": "Costco Groceries",
      "amount": 125.50,
      "currency": "USD",
      "category": 1,
      "billDate": "2025-07-10T00:00:00+00:00",
      "paidByUserId": "user-abc",
      "hasReceipt": true,
      "splitCount": 3,
      "createdAt": "2025-07-10T12:00:00+00:00"
    }
  ],
  ...
}
```

---

### 2. Get Bill by ID

```
GET /api/Bills/{id}
```

#### Response `200 OK` — `BillDetailDto`

```json
{
  "id": "01968b5a-...",
  "title": "Costco Groceries",
  "description": "Weekly grocery run",
  "amount": 125.50,
  "currency": "USD",
  "category": 1,
  "billDate": "2025-07-10T00:00:00+00:00",
  "paidByUserId": "user-abc",
  "receiptUrl": "/receipts/abc123.jpg",
  "relatedEntityId": "01968b5a-...",
  "relatedEntityType": "ShoppingList",
  "notes": "Used coupon for $5 off",
  "splits": [
    {
      "id": "01968b5a-...",
      "userId": "user-abc",
      "percentage": 33.33,
      "amount": 41.83,
      "status": 1,
      "paidAt": "2025-07-10T12:00:00+00:00"
    }
  ],
  "items": [
    {
      "id": "01968b5a-...",
      "name": "Organic Milk",
      "quantity": 2,
      "unitPrice": 4.99,
      "price": 9.98,
      "discount": 0.00
    }
  ],
  "createdAt": "2025-07-10T12:00:00+00:00",
  "createdBy": "user-abc",
  "lastModifiedAt": null
}
```

---

### 3. Get User Balances

```
GET /api/Bills/balances
```

Returns the net balance between the current user and other household members.

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `counterpartyUserId` | `string?` | — | Filter to a specific counterparty |

#### Response `200 OK` — `UserBalanceDto[]`

```json
[
  {
    "userId": "user-abc",
    "counterpartyUserId": "user-def",
    "netBalance": 25.00,
    "totalOwed": 50.00,
    "totalOwing": 25.00
  }
]
```

| Field | Type | Description |
|---|---|---|
| `userId` | `string` | Current user ID |
| `counterpartyUserId` | `string` | The other user |
| `netBalance` | `decimal` | Positive = they owe you, Negative = you owe them |
| `totalOwed` | `decimal` | Total amount the counterparty owes you |
| `totalOwing` | `decimal` | Total amount you owe the counterparty |

---

### 4. Get Spending Summary

```
GET /api/Bills/summary
```

Returns aggregated spending statistics for the current user.

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `fromDate` | `datetimeoffset?` | — | Start of date range |
| `toDate` | `datetimeoffset?` | — | End of date range |

#### Response `200 OK` — `SpendingSummaryDto`

```json
{
  "totalSpent": 1250.00,
  "totalOwed": 300.00,
  "totalOwing": 150.00,
  "netBalance": 150.00,
  "byCategory": [
    {
      "category": 1,
      "totalAmount": 800.00,
      "billCount": 12
    }
  ],
  "byUser": [
    {
      "userId": "user-def",
      "totalPaid": 500.00,
      "totalOwed": 100.00,
      "totalOwing": 50.00,
      "netBalance": 50.00
    }
  ]
}
```

---

### 5. Create Bill

```
POST /api/Bills
```

#### Request Body

```json
{
  "title": "Electric bill",
  "description": "July 2025",
  "amount": 95.00,
  "currency": "USD",
  "category": 2,
  "billDate": "2025-07-01T00:00:00+00:00",
  "notes": "Auto-paid from bank",
  "relatedEntityId": null,
  "relatedEntityType": null,
  "splits": [
    { "userId": "user-abc", "percentage": 50 },
    { "userId": "user-def", "percentage": 50 }
  ]
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `title` | `string` | ✅ | Max 256 chars |
| `description` | `string?` | — | Max 2000 chars |
| `amount` | `decimal` | ✅ | Must be > 0 |
| `currency` | `string` | ✅ | Max 3 chars (e.g., `USD`, `EUR`) |
| `category` | `int` | ✅ | Valid `BillCategory` |
| `billDate` | `datetimeoffset` | ✅ | When the bill was incurred |
| `notes` | `string?` | — | Max 2000 chars |
| `relatedEntityId` | `guid?` | — | Link to another entity (e.g., ShoppingList) |
| `relatedEntityType` | `string?` | — | Entity type name (max 256 chars) |
| `splits` | `BillSplitRequest[]` | ✅ | At least one split required. No duplicate user IDs. |

**BillSplitRequest:**

| Field | Type | Required | Constraints |
|---|---|---|---|
| `userId` | `string` | ✅ | User ID for this split |
| `percentage` | `decimal?` | — | Must be 0 < p ≤ 100. If any split has a percentage, ALL must, and they must total 100%. If omitted, split is equal. |

#### Response `201 Created`

```
Location: /api/Bills/{id}
```

Returns the new bill GUID.

---

### 6. Create Bill from Receipt *(AI-Powered)*

```
POST /api/Bills/from-receipt
```

Uploads a receipt image, uses AI (OpenAI) to extract line items, and creates a complete Bill entity.

**Content-Type:** `multipart/form-data`

#### Form Data

| Field | Type | Required | Constraints |
|---|---|---|---|
| `file` | binary | ✅ | JPEG, PNG, or WebP. Max 20 MB |

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `category` | `int` | `0` | `BillCategory` for the created bill |
| `splitUserIds` | `string?` | — | Comma-separated user IDs for equal split |

#### Response `201 Created` — `BillDetailDto`

Returns the fully populated bill including AI-extracted items.

---

### 7. Update Bill

```
PUT /api/Bills/{id}
```

**Authorization:** Requires edit access.

#### Request Body

```json
{
  "id": "01968b5a-...",
  "title": "Updated bill",
  "description": "Corrected amount",
  "amount": 100.00,
  "currency": "USD",
  "category": 2,
  "billDate": "2025-07-01T00:00:00+00:00",
  "notes": null
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | `guid` | ✅ | Must match route |
| `title` | `string` | ✅ | Max 256 chars |
| `description` | `string?` | — | Max 2000 chars |
| `amount` | `decimal` | ✅ | Must be > 0 |
| `currency` | `string` | ✅ | Max 3 chars |
| `category` | `int` | ✅ | Valid `BillCategory` |
| `billDate` | `datetimeoffset` | ✅ | Bill date |
| `notes` | `string?` | — | Max 2000 chars |

#### Response `204 No Content`

---

### 8. Delete Bill

```
DELETE /api/Bills/{id}
```

Soft-deletes the bill and all associated splits.

#### Response `204 No Content`

---

### 9. Get Receipt (Download)

```
GET /api/Bills/{id}/receipt
```

Downloads the receipt file attached to a bill.

#### Response `200 OK`

Returns the file as a binary stream with the appropriate `Content-Type` header.

#### Errors

| Code | Condition |
|---|---|
| `404` | Bill not found, or no receipt attached |

---

### 10. Add Receipt to Existing Bill

```
POST /api/Bills/{id}/receipt
```

Uploads and attaches a receipt image/PDF to an existing bill.

**Content-Type:** `multipart/form-data`

#### Form Data

| Field | Type | Required | Constraints |
|---|---|---|---|
| `file` | binary | ✅ | JPEG, PNG, WebP, or **PDF**. Must not be empty. |

#### Response `200 OK`

```json
{
  "receiptUrl": "/receipts/abc123.jpg"
}
```

---

### 11. Mark Split as Paid

```
PUT /api/Bills/{billId}/splits/{splitId}/pay
```

Marks a specific user's split as paid.

#### Response `204 No Content`

---

### Data Models — Bills

#### BillBriefDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Bill ID |
| `title` | `string` | Bill title |
| `amount` | `decimal` | Total amount |
| `currency` | `string` | Currency code |
| `category` | `int` | `BillCategory` enum value |
| `billDate` | `datetimeoffset` | When the bill was incurred |
| `paidByUserId` | `string` | Who paid the bill |
| `hasReceipt` | `bool` | Whether a receipt image is attached |
| `splitCount` | `int` | Number of bill splits |
| `createdAt` | `datetimeoffset` | Creation timestamp |

#### BillDetailDto

All fields from `BillBriefDto` plus:

| Field | Type | Description |
|---|---|---|
| `description` | `string?` | Bill description |
| `receiptUrl` | `string?` | URL to download the receipt |
| `relatedEntityId` | `guid?` | Linked entity (e.g., a ShoppingList) |
| `relatedEntityType` | `string?` | Type of linked entity |
| `notes` | `string?` | Additional notes |
| `splits` | `BillSplitDto[]` | How the bill is split |
| `items` | `BillItemDto[]` | Line items (from receipt or manual) |
| `createdBy` | `string?` | Creator user ID |
| `lastModifiedAt` | `datetimeoffset?` | Last modification timestamp |

#### BillItemDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Item ID |
| `name` | `string` | Item name |
| `quantity` | `int` | Quantity |
| `unitPrice` | `decimal` | Price per unit |
| `price` | `decimal` | Total price (`quantity × unitPrice`) |
| `discount` | `decimal` | Discount applied to this item |

#### BillSplitDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Split ID |
| `userId` | `string` | User who owes this split |
| `percentage` | `decimal` | Percentage of total bill |
| `amount` | `decimal` | Computed amount for this split |
| `status` | `int` | `SplitStatus` enum value |
| `paidAt` | `datetimeoffset?` | When this split was marked as paid |

---

## Shopping Lists API

> **Base URL:** `/api/ShoppingLists`  
> **Authentication:** Required (Bearer token)  
> **Sharing:** Lists can be shared via the [Shares API](#shares-api).

### Key Behaviors

- **Auto-completion:** When all items are checked, `isCompleted` automatically becomes `true` and `completedAt` is recorded. Unchecking any item reverts this.
- **Sort order:** New items get `max(sortOrder) + 1`. Items are returned sorted by `sortOrder` ascending.
- **Duplicate detection:** The `from-bill-item` endpoint rejects items with the same name (case-insensitive) — returns `409 Conflict`.

---

### 1. List Shopping Lists

```
GET /api/ShoppingLists
```

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `pageNumber` | `int` | `1` | Page number |
| `pageSize` | `int` | `20` | Items per page |
| `category` | `int?` | — | Filter by `ShoppingListCategory` |
| `isCompleted` | `bool?` | — | Filter by completion status |
| `searchTerm` | `string?` | — | Search in title and description |

#### Response `200 OK` — `PaginatedList<ShoppingListBriefDto>`

```json
{
  "items": [
    {
      "id": "01968b5a-...",
      "title": "Weekly Groceries",
      "category": 1,
      "dueDate": "2025-07-15",
      "isCompleted": false,
      "totalItems": 8,
      "checkedItems": 3,
      "createdAt": "2025-07-01T10:00:00+00:00"
    }
  ],
  ...
}
```

---

### 2. Get Shopping List by ID

```
GET /api/ShoppingLists/{id}
```

Returns full details including all items ordered by `sortOrder`.

#### Response `200 OK` — `ShoppingListDetailDto`

```json
{
  "id": "01968b5a-...",
  "title": "Weekly Groceries",
  "description": "Groceries for the family",
  "category": 1,
  "dueDate": "2025-07-15",
  "isCompleted": false,
  "completedAt": null,
  "items": [
    {
      "id": "01968b5a-...",
      "name": "Organic Milk",
      "quantity": 2,
      "unit": "liters",
      "notes": "Whole milk, not skim",
      "isChecked": false,
      "checkedAt": null,
      "checkedByUserId": null,
      "sortOrder": 0
    }
  ],
  "createdAt": "2025-07-01T10:00:00+00:00",
  "createdBy": "user-abc",
  "lastModifiedAt": "2025-07-02T14:30:00+00:00"
}
```

---

### 3. Create Shopping List

```
POST /api/ShoppingLists
```

#### Request Body

```json
{
  "title": "Weekly Groceries",
  "description": "For the week of July 14",
  "category": 1,
  "dueDate": "2025-07-15"
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `title` | `string` | ✅ | Max 256 chars |
| `description` | `string?` | — | Max 2000 chars |
| `category` | `int` | ✅ | Valid `ShoppingListCategory` |
| `dueDate` | `string?` | — | `yyyy-MM-dd` format |

#### Response `201 Created`

Returns the new shopping list GUID.

---

### 4. Update Shopping List

```
PUT /api/ShoppingLists/{id}
```

**Authorization:** Requires edit access.

#### Request Body

```json
{
  "id": "01968b5a-...",
  "title": "Updated Groceries",
  "description": "Changed the plan",
  "category": 2,
  "dueDate": "2025-07-20"
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | `guid` | ✅ | Must match route `{id}` |
| `title` | `string` | ✅ | Max 256 chars |
| `description` | `string?` | — | Max 2000 chars |
| `category` | `int` | ✅ | Valid `ShoppingListCategory` |
| `dueDate` | `string?` | — | `yyyy-MM-dd` format |

#### Response `204 No Content`

---

### 5. Delete Shopping List

```
DELETE /api/ShoppingLists/{id}
```

Soft-deletes the list and all its items. **Authorization:** Requires edit access.

#### Response `204 No Content`

---

### 6. Add Item

```
POST /api/ShoppingLists/{id}/items
```

**Authorization:** Requires edit access.

#### Request Body

```json
{
  "shoppingListId": "01968b5a-...",
  "name": "Eggs",
  "quantity": 12,
  "unit": "pcs",
  "notes": "Free-range preferred"
}
```

> ⚠️ `shoppingListId` **must** match the route `{id}`.

| Field | Type | Required | Constraints |
|---|---|---|---|
| `shoppingListId` | `guid` | ✅ | Must match route |
| `name` | `string` | ✅ | Max 500 chars |
| `quantity` | `int` | — | Default `1`, must be > 0 |
| `unit` | `string?` | — | Max 50 chars |
| `notes` | `string?` | — | Max 1000 chars |

#### Response `201 Created` — `ShoppingItemDto`

---

### 7. Update Item

```
PUT /api/ShoppingLists/{id}/items/{itemId}
```

**Authorization:** Requires edit access.

#### Request Body

```json
{
  "shoppingListId": "01968b5a-...",
  "itemId": "01968b5a-...",
  "name": "Organic Eggs",
  "quantity": 6,
  "unit": "pcs",
  "notes": "Free-range, organic",
  "sortOrder": 2
}
```

> ⚠️ Both `shoppingListId` and `itemId` **must** match the route parameters.

| Field | Type | Required | Constraints |
|---|---|---|---|
| `shoppingListId` | `guid` | ✅ | Must match route `{id}` |
| `itemId` | `guid` | ✅ | Must match route `{itemId}` |
| `name` | `string` | ✅ | Max 500 chars |
| `quantity` | `int` | — | Default `1`, must be > 0 |
| `unit` | `string?` | — | Max 50 chars |
| `notes` | `string?` | — | Max 1000 chars |
| `sortOrder` | `int` | — | Must be ≥ 0 |

#### Response `204 No Content`

---

### 8. Remove Item

```
DELETE /api/ShoppingLists/{id}/items/{itemId}
```

Permanently removes an item. **Authorization:** Requires edit access.

#### Response `204 No Content`

---

### 9. Toggle Item (Check/Uncheck)

```
PUT /api/ShoppingLists/{id}/items/{itemId}/toggle
```

Toggles the checked state. **Authorization:** Requires edit access.

#### Behavior

- **Check:** Sets `isChecked = true`, `checkedAt = now`, `checkedByUserId = current user`
- **Uncheck:** Clears `isChecked`, `checkedAt`, `checkedByUserId`
- **Auto-completion:** If all items become checked → list marked as completed. If any item unchecked → completion reverts.

#### Response `204 No Content`

---

### 10. Add Item from Bill Item

```
POST /api/ShoppingLists/{id}/items/from-bill-item
```

Copies a `BillItem` into the shopping list. Useful for "re-buying" something from a previous bill.

**Authorization:** Requires edit access.

#### Request Body

```json
{
  "shoppingListId": "01968b5a-...",
  "billItemId": "01968b5a-...",
  "quantityOverride": 3,
  "unitOverride": "kg"
}
```

> ⚠️ `shoppingListId` **must** match the route `{id}`.

| Field | Type | Required | Constraints |
|---|---|---|---|
| `shoppingListId` | `guid` | ✅ | Must match route |
| `billItemId` | `guid` | ✅ | Must reference an existing `BillItem` |
| `quantityOverride` | `int?` | — | Must be > 0 if provided |
| `unitOverride` | `string?` | — | Max 50 chars |

#### Behavior

- **Name** copied from `BillItem.Name`
- **Quantity** defaults to `BillItem.Quantity` unless `quantityOverride` provided
- **Notes** auto-generated with original unit price for reference
- **Duplicate detection:** Same name (case-insensitive) → `409 Conflict`
- **Completion reset:** If list was completed, it's automatically uncompleted

#### Response `201 Created` — `ShoppingItemDto`

---

### 11. Process Receipt *(AI-Powered)*

```
POST /api/ShoppingLists/{id}/process-receipt
```

Uploads a receipt, AI-analyzes it, reconciles items with the shopping list, and creates a linked Bill.

**Content-Type:** `multipart/form-data`  
**Authorization:** Requires edit access.

#### Form Data

| Field | Type | Required | Constraints |
|---|---|---|---|
| `file` | binary | ✅ | JPEG, PNG, or WebP. Max 20 MB |

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `splitUserIds` | `string?` | — | Comma-separated user IDs for equal bill split |

#### Behavior

1. **AI Analysis** — Receipt image sent to OpenAI with existing shopping list item names for smart matching
2. **Bill Creation** — A `Bill` is created with `relatedEntityId` = shopping list ID, `relatedEntityType` = `"ShoppingList"`
3. **Item Reconciliation:**
   - Receipt item **matches unchecked** list item → item is checked off
   - Receipt item **matches already checked** item → skipped
   - Receipt item **has no match** → new `ShoppingItem` added (unchecked)
4. **Auto-completion** — If all items are now checked, list is marked complete

#### Response `201 Created` — `ProcessReceiptResultDto`

```json
{
  "billId": "01968b5a-...",
  "bill": { /* BillDetailDto */ },
  "checkedItems": [
    {
      "id": "01968b5a-...",
      "name": "Spaghetti Pasta",
      "isChecked": true,
      "checkedAt": "2025-07-10T12:00:00+00:00",
      "checkedByUserId": "user-abc",
      ...
    }
  ],
  "addedItems": [
    {
      "id": "01968b5a-...",
      "name": "Bread",
      "quantity": 2,
      "isChecked": false,
      "notes": "Added from receipt (unit price: $2.50)",
      ...
    }
  ]
}
```

| Field | Type | Description |
|---|---|---|
| `billId` | `guid` | ID of the created bill |
| `bill` | `BillDetailDto` | Full bill detail including items and splits |
| `checkedItems` | `ShoppingItemDto[]` | Existing items that were matched and checked off |
| `addedItems` | `ShoppingItemDto[]` | New items added from unmatched receipt items |

---

### Data Models — Shopping Lists

#### ShoppingListBriefDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Shopping list ID |
| `title` | `string` | List title |
| `category` | `int` | `ShoppingListCategory` enum value |
| `dueDate` | `string?` | Optional due date (`yyyy-MM-dd`) |
| `isCompleted` | `bool` | `true` when every item is checked |
| `totalItems` | `int` | Total item count |
| `checkedItems` | `int` | Checked item count |
| `createdAt` | `datetimeoffset` | Creation timestamp |

#### ShoppingListDetailDto

All fields from `ShoppingListBriefDto` plus:

| Field | Type | Description |
|---|---|---|
| `description` | `string?` | Optional description |
| `completedAt` | `datetimeoffset?` | When auto-completed |
| `items` | `ShoppingItemDto[]` | Items sorted by `sortOrder` ascending |
| `createdBy` | `string?` | Creator user ID |
| `lastModifiedAt` | `datetimeoffset?` | Last modification timestamp |

#### ShoppingItemDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Item ID |
| `name` | `string` | Item name |
| `quantity` | `int` | Quantity needed |
| `unit` | `string?` | Unit of measure (e.g., `kg`, `liters`, `pcs`) |
| `notes` | `string?` | Free-text notes |
| `isChecked` | `bool` | Whether checked off |
| `checkedAt` | `datetimeoffset?` | When checked |
| `checkedByUserId` | `string?` | Who checked it |
| `sortOrder` | `int` | Display order (0-based, ascending) |

---

## Shares API

> **Base URL:** `/api/Shares`  
> **Authentication:** Required (Bearer token)

The sharing system enables collaborative access to entities (Tasks, Bills, Shopping Lists, Notifications).

### Supported Entity Types

| Value | Constant | Description |
|---|---|---|
| `"HouseholdTask"` | `EntityTypes.HouseholdTask` | Share a task |
| `"Bill"` | `EntityTypes.Bill` | Share a bill |
| `"ShoppingList"` | `EntityTypes.ShoppingList` | Share a shopping list |
| `"Notification"` | `EntityTypes.Notification` | Share a notification |

---

### 1. Get Shares for an Entity

```
GET /api/Shares?entityType=HouseholdTask&entityId=01968b5a-...
```

#### Query Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `entityType` | `string` | ✅ | One of the supported entity types |
| `entityId` | `guid` | ✅ | The entity's ID |

#### Response `200 OK` — `ShareDto[]`

```json
[
  {
    "id": "01968b5a-...",
    "entityId": "01968b5a-...",
    "entityType": "HouseholdTask",
    "sharedWithUserId": "user-def",
    "permission": 1,
    "createdAt": "2025-07-01T12:00:00+00:00",
    "createdBy": "user-abc"
  }
]
```

---

### 2. Share an Entity

```
POST /api/Shares
```

#### Request Body

```json
{
  "entityType": "ShoppingList",
  "entityId": "01968b5a-...",
  "sharedWithUserId": "user-def",
  "permission": 1
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `entityType` | `string` | ✅ | Must be a supported entity type |
| `entityId` | `guid` | ✅ | Must be a valid entity ID |
| `sharedWithUserId` | `string` | ✅ | Target user ID |
| `permission` | `int` | ✅ | Valid `SharePermission` value |

#### Response `201 Created`

Returns the new share GUID.

---

### 3. Update Share Permission

```
PUT /api/Shares/{id}
```

#### Request Body

```json
{
  "permission": 0
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `permission` | `int` | ✅ | Valid `SharePermission` value |

#### Response `204 No Content`

---

### 4. Revoke Share

```
DELETE /api/Shares/{id}
```

Removes the share entirely.

#### Response `204 No Content`

---

### Data Models — Shares

#### ShareDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Share ID |
| `entityId` | `guid` | The shared entity's ID |
| `entityType` | `string` | Entity type string |
| `sharedWithUserId` | `string` | User who has access |
| `permission` | `int` | `SharePermission` enum value |
| `createdAt` | `datetimeoffset` | When the share was created |
| `createdBy` | `string?` | User who created the share |

---

## Notifications API

> **Base URL:** `/api/Notifications`  
> **Authentication:** Required (Bearer token)

Notifications are created automatically by the system when events occur (task assigned, bill created, item checked, etc.). They can also be created manually.

---

### 1. List Notifications

```
GET /api/Notifications
```

#### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `pageNumber` | `int` | `1` | Page number |
| `pageSize` | `int` | `20` | Items per page |
| `isRead` | `bool?` | — | Filter by read status |
| `type` | `int?` | — | Filter by `NotificationType` |

#### Response `200 OK` — `PaginatedList<NotificationBriefDto>`

```json
{
  "items": [
    {
      "id": "01968b5a-...",
      "title": "Item checked: Organic Milk",
      "type": 18,
      "fromUserId": "user-abc",
      "isRead": false,
      "createdAt": "2025-07-10T12:00:00+00:00"
    }
  ],
  ...
}
```

---

### 2. Get Notification by ID

```
GET /api/Notifications/{id}
```

#### Response `200 OK` — `NotificationDetailDto`

```json
{
  "id": "01968b5a-...",
  "title": "Item checked: Organic Milk",
  "description": "John Doe checked off Organic Milk from Weekly Groceries",
  "type": 18,
  "fromUserId": "user-abc",
  "toUserId": "user-def",
  "relatedEntityId": "01968b5a-...",
  "relatedEntityType": "ShoppingList",
  "isRead": false,
  "readAt": null,
  "createdAt": "2025-07-10T12:00:00+00:00"
}
```

---

### 3. Get Unread Count

```
GET /api/Notifications/unread-count
```

#### Response `200 OK`

```json
5
```

Returns a plain integer.

---

### 4. Create Notification

```
POST /api/Notifications
```

#### Request Body

```json
{
  "title": "Reminder",
  "description": "Don't forget to buy milk",
  "type": 0,
  "toUserId": "user-def",
  "relatedEntityId": "01968b5a-...",
  "relatedEntityType": "ShoppingList"
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `title` | `string` | ✅ | Max 256 chars |
| `description` | `string?` | — | Max 2000 chars |
| `type` | `int` | ✅ | Valid `NotificationType` value |
| `toUserId` | `string` | ✅ | Recipient user ID (max 450 chars) |
| `relatedEntityId` | `guid?` | — | Link to related entity |
| `relatedEntityType` | `string?` | — | Max 256 chars |

#### Response `201 Created`

Returns the new notification GUID.

---

### 5. Mark as Read

```
PUT /api/Notifications/{id}/read
```

#### Response `204 No Content`

---

### 6. Mark All as Read

```
PUT /api/Notifications/read-all
```

#### Response `200 OK`

```json
12
```

Returns the count of notifications that were marked as read.

---

### 7. Delete Notification

```
DELETE /api/Notifications/{id}
```

#### Response `204 No Content`

---

### Data Models — Notifications

#### NotificationBriefDto

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Notification ID |
| `title` | `string` | Notification title |
| `type` | `int` | `NotificationType` enum value |
| `fromUserId` | `string?` | Who triggered the notification |
| `isRead` | `bool` | Whether read |
| `createdAt` | `datetimeoffset` | When created |

#### NotificationDetailDto

All fields from `NotificationBriefDto` plus:

| Field | Type | Description |
|---|---|---|
| `description` | `string?` | Notification description |
| `toUserId` | `string` | Recipient user ID |
| `relatedEntityId` | `guid?` | Linked entity for navigation |
| `relatedEntityType` | `string?` | Entity type for navigation |
| `readAt` | `datetimeoffset?` | When marked as read |

---

## Enumerations Reference

All enum values are sent and received as **integers**. Never send the string name.

### TaskCategory

| Value | Name | Description |
|---|---|---|
| `0` | `General` | General purpose |
| `1` | `Cleaning` | Cleaning tasks |
| `2` | `Maintenance` | Home maintenance |
| `3` | `Cooking` | Meal preparation |
| `4` | `Gardening` | Yard and garden |
| `5` | `Laundry` | Laundry |
| `6` | `Shopping` | Shopping errands |
| `7` | `PetCare` | Pet care |
| `8` | `ChildCare` | Child care |
| `9` | `Organization` | Home organization |

### TaskPriority

| Value | Name |
|---|---|
| `0` | `Low` |
| `1` | `Medium` |
| `2` | `High` |
| `3` | `Critical` |

### RecurrenceType

| Value | Name | Example |
|---|---|---|
| `0` | `Daily` | Every N days |
| `1` | `Weekly` | Every N weeks |
| `2` | `Monthly` | Every N months |
| `3` | `Yearly` | Every N years |

### OccurrenceStatus

| Value | Name | Description |
|---|---|---|
| `0` | `Pending` | Not yet started |
| `1` | `InProgress` | Currently being worked on |
| `2` | `Completed` | Done |
| `3` | `Skipped` | Skipped by user |
| `4` | `Overdue` | Past due date, not completed |

### BillCategory

| Value | Name |
|---|---|
| `0` | `General` |
| `1` | `Groceries` |
| `2` | `Utilities` |
| `3` | `Rent` |
| `4` | `Maintenance` |
| `5` | `Supplies` |
| `6` | `Internet` |
| `7` | `Insurance` |
| `8` | `Furniture` |
| `9` | `Cleaning` |
| `10` | `Other` |

### ShoppingListCategory

| Value | Name |
|---|---|
| `0` | `General` |
| `1` | `Groceries` |
| `2` | `Household` |
| `3` | `Personal` |
| `4` | `Health` |
| `5` | `Electronics` |
| `6` | `Clothing` |
| `7` | `Other` |

### SplitStatus

| Value | Name | Description |
|---|---|---|
| `0` | `Unpaid` | Split not yet paid |
| `1` | `Paid` | Marked as paid |
| `2` | `Settled` | Fully settled |

### SharePermission

| Value | Name | Grants |
|---|---|---|
| `0` | `View` | Read-only access |
| `1` | `Edit` | Full read/write access |

### NotificationType

| Value | Name | Triggered By |
|---|---|---|
| `0` | `General` | Manual notification |
| `1` | `TaskAssigned` | Task creation/update with assignee |
| `2` | `TaskUpdated` | Task details changed |
| `3` | `TaskDeleted` | Task soft-deleted |
| `4` | `TaskDueSoon` | Upcoming task due date |
| `5` | `OccurrenceCompleted` | Occurrence marked complete |
| `6` | `OccurrenceSkipped` | Occurrence skipped |
| `7` | `ShareReceived` | Entity shared with user |
| `8` | `ShareRevoked` | Share access removed |
| `9` | `Mention` | User mentioned |
| `10` | `BillCreated` | New bill created |
| `11` | `BillUpdated` | Bill details changed |
| `12` | `BillDeleted` | Bill soft-deleted |
| `13` | `BillSplitPaid` | Bill split marked as paid |
| `14` | `BillReceiptAdded` | Receipt attached to bill |
| `15` | `ShoppingListCreated` | New shopping list created |
| `16` | `ShoppingListUpdated` | Shopping list details changed |
| `17` | `ShoppingListDeleted` | Shopping list soft-deleted |
| `18` | `ShoppingItemChecked` | Shopping item toggled to checked |

---

## Real-Time (SignalR)

The API provides two SignalR hubs for real-time updates. Connect after authentication.

### Hubs

| Hub URL | Purpose | Auth |
|---|---|---|
| `/hubs/notifications` | User notifications (all entity types) | Bearer token required |
| `/hubs/tasks` | Task-specific real-time updates | Bearer token required |

### Notification Hub — `/hubs/notifications`

Users are automatically added to a group named `user-{userId}` on connect. No manual subscription needed.

#### Events Received

| Event | Payload Type | When |
|---|---|---|
| `NotificationCreatedEvent` | See below | Any notification-triggering action |

#### Payload Shape

```json
{
  "eventType": "NotificationCreatedEvent",
  "notificationId": "01968b5a-...",
  "title": "Item checked: Organic Milk",
  "occurredAt": "2025-07-01T12:00:00+00:00"
}
```

> **Who receives it?** The owner + all shared users of the related entity, **excluding** the user who performed the action.

### Task Hub — `/hubs/tasks`

Supports manual group subscription for task-level updates.

#### Client → Server Methods

| Method | Parameters | Description |
|---|---|---|
| `JoinTaskGroup` | `taskId: guid` | Subscribe to updates for a specific task |
| `LeaveTaskGroup` | `taskId: guid` | Unsubscribe from a task's updates |

### Connection Example (JavaScript)

```javascript
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('/hubs/notifications', {
    accessTokenFactory: () => getAccessToken()
  })
  .withAutomaticReconnect()
  .build();

connection.on('NotificationCreatedEvent', (notification) => {
  console.log('New notification:', notification);
  // Update UI, show toast, increment badge count, etc.
});

await connection.start();
```

---

## File Uploads

Several endpoints accept file uploads via `multipart/form-data`.

### Upload Constraints

| Constraint | Value |
|---|---|
| Max file size | **20 MB** |
| Allowed image types | `image/jpeg`, `image/png`, `image/webp` |
| Allowed receipt types (Add Receipt) | `image/jpeg`, `image/png`, `image/webp`, `application/pdf` |
| Form field name | `file` |

### Endpoints Accepting Files

| Endpoint | Allowed Types |
|---|---|
| `POST /api/Bills/from-receipt` | JPEG, PNG, WebP |
| `POST /api/Bills/{id}/receipt` | JPEG, PNG, WebP, **PDF** |
| `POST /api/ShoppingLists/{id}/process-receipt` | JPEG, PNG, WebP |

### cURL Example

```bash
curl -X POST "https://<host>/api/ShoppingLists/{id}/process-receipt?splitUserIds=user1,user2" \
  -H "Authorization: Bearer <token>" \
  -F "file=@receipt.jpg"
```

### JavaScript Fetch Example

```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);

const response = await fetch(`/api/ShoppingLists/${listId}/process-receipt`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
    // Do NOT set Content-Type — the browser sets it with the boundary
  },
  body: formData
});
```

---

## Authorization & Sharing Model

### Access Levels

| Actor | Read | Write | Delete |
|---|---|---|---|
| **Owner** (creator) | ✅ | ✅ | ✅ |
| **Shared — View** permission | ✅ | ❌ | ❌ |
| **Shared — Edit** permission | ✅ | ✅ | ✅ |
| **No relationship** | ❌ `403` | ❌ `403` | ❌ `403` |

### How Sharing Works

1. **Create** an entity (Task, Bill, Shopping List)
2. **Share** via `POST /api/Shares` with the target user ID and permission level
3. The shared user can now access the entity based on the permission
4. The shared user receives a `ShareReceived` notification (type `7`)
5. **List queries** (e.g., `GET /api/Tasks`) automatically include entities shared with the current user

### Entity Types for Sharing

Use the exact string values when calling the Shares API:

```
"HouseholdTask"  — for tasks
"Bill"           — for bills
"ShoppingList"   — for shopping lists
"Notification"   — for notifications
```

---

## Cross-Feature Workflows

### Complete Shopping Workflow

```
1. POST   /api/ShoppingLists                       → Create list
2. POST   /api/Shares                              → Share with household
3. POST   /api/ShoppingLists/{id}/items            → Add items
4. PUT    /api/ShoppingLists/{id}/items/{x}/toggle  → Check items while shopping
5. POST   /api/ShoppingLists/{id}/process-receipt   → Upload receipt after shopping
   → Bill created, items reconciled, notifications sent
6. GET    /api/ShoppingLists/{id}                   → View final state
7. GET    /api/Bills/{billId}                       → View the linked bill
```

### Task with Recurrence Workflow

```
1. POST   /api/Tasks                                → Create recurring task
   { isRecurring: true, recurrenceType: 1, interval: 1, ... }
2. GET    /api/Occurrences/by-task/{taskId}          → View generated occurrences
3. POST   /api/Occurrences/{id}/complete             → Complete today's occurrence
4. POST   /api/Occurrences/{id}/skip                 → Skip if unavailable
```

### Bill Splitting Workflow

```
1. POST   /api/Bills                                → Create bill with splits
2. GET    /api/Bills/balances                        → View who owes what
3. PUT    /api/Bills/{id}/splits/{splitId}/pay        → Mark a split as paid
4. GET    /api/Bills/summary                         → View spending summary
```

### Re-buying from a Previous Bill

```
1. GET    /api/Bills/{billId}                        → Get bill with items[]
2. POST   /api/ShoppingLists                         → Create new shopping list
3. POST   /api/ShoppingLists/{id}/items/from-bill-item → For each bill item
   → 409 Conflict if item name already exists (skip or rename)
```

---

## Health Check

```
GET /health
```

**No authentication required.** Returns `200 OK` with `Healthy` if the database is reachable.

---

## OpenAPI / Swagger

```
GET /openapi/v1.json
```

The API exposes an OpenAPI specification. Use this for auto-generating TypeScript clients, Postman collections, etc.

---

## Notes for Front-End Developers

1. **All enum fields are integers.** Send numeric values, not string names. Use the [Enumerations Reference](#enumerations-reference) to map them.

2. **IDs are GUIDs (v7).** Time-sortable. Default ordering by ID descending ≈ newest first.

3. **Route/body ID matching is enforced.** All `PUT` and `POST` endpoints with IDs in both route and body will reject mismatches with `400`.

4. **Dates:**
   - `DateOnly` fields (tasks, shopping lists): use `yyyy-MM-dd` format
   - `DateTimeOffset` fields (bills, timestamps): use ISO 8601 format with timezone (e.g., `2025-07-01T10:00:00+00:00`)

5. **Soft deletes are invisible.** Deleted entities return `404`. They don't appear in list queries. There is no restore endpoint.

6. **Pagination defaults.** `pageNumber=1, pageSize=20`. Max page size is not enforced server-side, but keep it reasonable.

7. **File uploads use `multipart/form-data`.** Do NOT set `Content-Type: application/json` for these endpoints. The browser/client should set the multipart boundary automatically.

8. **The `traceId` in error responses** is invaluable for debugging. Log it or display it in dev tools.

9. **SignalR auto-reconnect.** Use `withAutomaticReconnect()` when building the connection. The notification hub auto-joins the user group on connect.

10. **Rate limit:** 60 requests per minute. Display a friendly message on `429` responses. The `Retry-After` header may be present.

11. **Toggle is a flip.** `PUT .../toggle` always inverts the current state. If you need to set a specific state, read the current state first via `GET`.

12. **Shopping list auto-completion** is automatic. Don't build UI to manually mark a list as complete — it happens when the last item is checked.

13. **Bill `relatedEntityId` / `relatedEntityType`** links bills to other entities (mainly shopping lists). Use these fields to navigate between related resources.

14. **AI receipt processing** is smart about matching. Abbreviated receipt text (e.g., "ORG BNS CHKN") is normalized to match existing list item names (e.g., "Organic Boneless Chicken"). Already-checked items are not duplicated.

15. **Self-service vs admin endpoints.** Users can manage their own profile via `/api/Users/me`. Only `Administrator`-role users can access `/api/Users`, `/api/Users/{id}`, etc.
