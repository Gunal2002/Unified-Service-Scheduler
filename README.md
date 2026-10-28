# Unified Service Scheduler

A backend implementation for Keyloop Technical Assessment — **Scenario A: The Unified Service Scheduler**.

## Overview

The Unified Service Scheduler exposes a REST API for requesting vehicle service appointments. Before confirming a booking, it validates the request and checks that a qualified technician and an active service bay are available for the entire service duration. A successful booking is persisted with its customer, vehicle, dealership, service type, technician, and service bay associations.

## Key features

- Create a confirmed service appointment.
- Validate required IDs and future appointment time.
- Verify that the customer and dealership exist.
- Verify that the vehicle belongs to the supplied customer.
- Verify that the service type belongs to the selected dealership.
- Calculate the appointment end time from the service duration.
- Match technicians using the service's required skill.
- Check technician and service-bay availability across the full appointment interval.
- Persist appointment records in SQLite.
- Retrieve an appointment by ID.
- Return meaningful HTTP responses for success, validation failures, missing records, and resource conflicts.
- Automated tests for core domain and booking-service behavior.

## Technology stack

| Technology | Purpose |
|---|---|
| C# / .NET 8 | Application runtime and implementation language |
| ASP.NET Core Web API | REST endpoints and HTTP response handling |
| Entity Framework Core 8 | Object-relational mapping and migrations |
| SQLite | Persistent relational database for the assessment |
| Swagger / OpenAPI | Interactive API exploration and manual testing |
| xUnit | Automated tests |

## Solution structure

Update this tree to match the final repository:

```text
Keyloop.ServiceScheduler/
├── Keyloop.ServiceScheduler.Api/
│   ├── Controllers/
│   ├── Contracts/
│   ├── Data/
│   ├── Services/
│   ├── Migrations/
│   └── Program.cs
├── Keyloop.ServiceScheduler.Core/
│   └── Entities/
└── Keyloop.ServiceScheduler.Tests/
```

## Prerequisites

- .NET 8 SDK
- Visual Studio 2022 with the ASP.NET and web development workload, or a compatible command-line environment
- Git (for cloning the repository)

Check the SDK:

```bash
dotnet --info
```

## Build

Run from the solution directory:

```bash
dotnet restore
dotnet build
```

## Run the API

From the solution directory, run:

```bash
dotnet run --project Keyloop.ServiceScheduler.Api
```

Use the launch URL printed in the terminal. Open the Swagger URL configured by the project (commonly `/swagger`) to test the API.

> Database migration and seed behavior depends on the application's startup configuration. Confirm whether the app applies migrations and seeds demo data automatically; if not, follow the setup implemented in the repository.

## API endpoints

### Create an appointment

`POST /api/Appointments`

Example request (replace the GUID placeholders with IDs that exist in your seeded database):

```json
{
  "dealershipId": "<dealership-guid>",
  "customerId": "<customer-guid>",
  "vehicleId": "<vehicle-guid>",
  "serviceTypeId": "<service-type-guid>",
  "startTime": "2026-10-28T10:00:00+05:30"
}
```

Possible responses:

- `201 Created` — appointment persisted successfully.
- `400 Bad Request` — invalid request or invalid business references.
- `409 Conflict` — no qualified technician or service bay is available for the requested interval.

### Retrieve an appointment

`GET /api/Appointments/{id}`

Possible responses:

- `200 OK` — appointment found.
- `404 Not Found` — appointment does not exist.

## Booking rules

1. The requested start time must be provided and in the future.
2. The customer and dealership must exist.
3. The selected vehicle must belong to the supplied customer.
4. The service type must belong to the selected dealership.
5. The service duration determines the appointment end time.
6. A technician must possess the required skill and be free for the whole interval.
7. An active service bay must be free for the whole interval.
8. Only confirmed appointments reserve resources in the current implementation.

### Interval overlap

The overlap rule is:

```text
requestedStart < existingEnd && requestedEnd > existingStart
```

This allows adjacent appointments, such as 10:00–11:00 and 11:00–11:30, while rejecting overlapping intervals.

## Run automated tests

From the solution directory:

```bash
dotnet test
```

The final test count should be taken from the actual test run rather than hard-coded here.

## Manual verification checklist

- [ ] Valid appointment returns `201 Created`.
- [ ] Retrieving the created appointment returns `200 OK`.
- [ ] Retrieving an unknown appointment ID returns `404 Not Found`.
- [ ] A past appointment returns `400 Bad Request`.
- [ ] A booking with no available qualified technician or bay returns `409 Conflict`.
- [ ] An appointment remains retrievable after restarting the API while using the same SQLite database file.
- [ ] `dotnet build` and `dotnet test` pass on the final commit.

## Architecture

The solution is a modular monolith:

- **AppointmentsController:** handles HTTP requests and response mapping.
- **AppointmentBookingService:** owns validation, technician qualification, resource availability, and booking creation.
- **SchedulerDbContext:** uses EF Core for data access.
- **SQLite:** stores persistent application data.
- **Core entities:** model appointments and the related dealership resources.

See the accompanying System Design Document for the architecture diagram, sequence diagram, ER diagram, availability flowchart, data flow, assumptions, observability strategy, and production roadmap.

## Design trade-offs and limitations

- SQLite is used to simplify local setup and demonstration; production deployment may require a database designed for higher concurrency.
- The static `SemaphoreSlim` serializes booking operations only within one API process. It does not provide distributed concurrency protection across multiple processes or instances.
- The implementation loads confirmed appointments for a dealership and compares overlaps in memory. This is acceptable for a small assessment dataset but should be optimized for larger workloads.
- Authentication, dealership-level authorization, cancellation, rescheduling, operating hours, notifications, metrics, and distributed tracing are not claimed as implemented unless present in the final code.

### Production improvements

- Add a shared database-backed transaction and reservation strategy that prevents double booking across instances.
- Add concurrent integration tests for resource conflicts.
- Add authentication and dealership-scoped authorization.
- Optimize overlap queries and indexes.
- Add operating-hours and time-zone policies.
- Add structured business metrics, tracing, and health checks.

## Observability

ASP.NET Core logging can be used to diagnose request and application failures. For a production service, useful additions include structured booking outcome logs, request latency and error metrics, tracing across the API and database, and liveness/readiness health checks. Describe only the observability capabilities that are actually configured as implemented.

## GenAI Collaboration Narrative

### How AI was directed

I used GenAI as an engineering collaborator to help translate the assessment brief into an implementation plan, draft code and tests, reason through edge cases, and review design trade-offs.

- I broke the acceptance criteria into smaller tasks: validation, technician skill matching, bay availability, duration-based overlap checks, persistence, and HTTP response handling.
- I requested focused, reviewable changes instead of accepting a large unverified implementation all at once.
- I asked for explicit reasoning around adjacent appointments, overlap boundaries, invalid references, and the distinction between validation failures and resource conflicts.
- I kept the architecture proportionate to the assessment and documented production improvements separately from the implemented scope.

### How the output was verified

- Built the solution and ran the automated test suite.
- Used Swagger to test a successful booking and inspect the returned appointment and assigned resources.
- Tested the GET endpoint for existing and unknown IDs.
- Verified that past appointment times were rejected and unavailable-resource requests returned a conflict.
- Restarted the API and retrieved the saved appointment to verify persistence.
- Reviewed the concurrency limitation of a process-local semaphore and documented that it is not a distributed lock.

### Ownership and reflection

GenAI accelerated drafting and exploration, but I remain responsible for the requirements, the code I submit, the tests, and the architectural trade-offs. AI-generated suggestions were treated as proposals to verify through builds, tests, API behavior, and code review. Before submission, I will rerun the checks on the final commit and ensure that all claims in this README match the actual repository.

> Personalize this narrative with one concrete example of an issue you investigated and the evidence you used to verify the fix. Do not claim a debugging step that you did not perform.

## Assessment deliverables

- System Design Document
- Git repository containing the implementation and tests
- This README with setup, API, test, design, and AI collaboration details
- 5–10 minute video walkthrough covering the scenario, design, implementation, AI collaboration, demo, and lessons learned
