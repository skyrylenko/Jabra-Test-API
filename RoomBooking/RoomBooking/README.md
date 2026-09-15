# Room Booking API

A simple REST API for managing conference room reservations.

## Technology

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Swagger / OpenAPI

## Running the application

From the repository root:

```bash
dotnet restore
dotnet run --project RoomBooking/RoomBooking/RoomBooking.csproj
```

The application uses a local SQLite database named `roombooking.db`.

Swagger UI:

```text
https://localhost:7075/swagger
```

## API endpoints

### Get all rooms

```http
GET /api/rooms
```

### Get available rooms

```http
GET /api/rooms?from=2026-09-17T10:00:00&to=2026-09-17T11:00:00
```

### Get room reservations

```http
GET /api/rooms/{roomId}/reservations
```

Optional time range:
```http
GET /api/rooms/{roomId}/reservations?from=2026-09-17T10:00:00&to=2026-09-17T11:00:00
```

### Create a reservation

```http
POST /api/rooms/{roomId}/reservations
Content-Type: application/json
```

Request body:

```json
{
  "start": "2026-09-17T10:00:00",
  "end": "2026-09-17T11:00:00",
  "title": "Team meeting"
}
```

## Reservation conflict handling

Two reservations conflict when:
```text
existing.Start < new.End
AND
existing.End > new.Start
```

Overlapping reservations return `409 Conflict`.
Adjacent reservations are allowed. For example:
10:00–11:00
11:00–12:00
## Validation


The API validates:
- required reservation title;
- title length from 1 to 300 characters;
- start time before end time;
- room existence;
- reservation conflicts.

## Data and performance decisions

- AsNoTracking() is used for read-only queries.
- Reservations are queried by room and time range.
- An index is configured for RoomId, Start, and End.
- SQLite is used as a lightweight local database.
- Sample rooms are created automatically on first startup.

## Known limitations

This is a small recruitment task implementation. A production version could additionally include:
- authentication and authorization;
- concurrency handling for simultaneous booking requests;
- database migrations;
- structured logging;
- global exception handling;
- pagination;
- unit and integration tests were not implemented because they were not requested for this task and the implementation time was limited;
- time zone policy and stronger UTC validation.

## Design patterns and approach

The project uses a lightweight layered structure:

- **API layer** — controllers handle HTTP requests and responses.
- **Application layer** — `ReservationService` contains reservation business rules.
- **Domain layer** — `Room` and `Reservation` represent core domain entities.
- **Infrastructure layer** — `RoomBookingDbContext` configures EF Core and SQLite.
- **Contracts** — DTOs define API input, output, and query models.

The following programming patterns and approaches are used:

- **Dependency Injection** — ASP.NET Core provides the `DbContext` and application services.
- **Service Layer** — reservation business logic is separated from the controller.
- **DTO pattern** — API contracts are separated from persistence entities.
- **Result pattern** — expected reservation outcomes are represented by `ReservationCreationResult` and `ReservationCreationStatus`.
- **Unit of Work** — `RoomBookingDbContext` coordinates persistence through `SaveChangesAsync()`.
- **LINQ query expressions** — filtering and conflict detection are expressed as database queries.

No additional GoF patterns were introduced because the scope of the task is intentionally small.
