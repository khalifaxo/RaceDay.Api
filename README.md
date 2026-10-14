# RaceDay – Event Management System

RaceDay is a web-based event management system for South African road running, walking and cycling events. Organisers create and manage events and capture results. Participants browse events, enter them, and track their own results.

It is built across three parts:

- **Part 1** – ERD, API endpoint plan and SQL script (`/docs`)
- **Part 2** – ASP.NET Core Web API with EF Core, session authentication, Swagger and unit tests (`/RaceDay.Api`)
- **Part 3** – MVC front end, Azure Blob Storage and Docker (to follow)

## User Roles

**Organiser**
- Creates, edits and deletes their own events
- Defines categories and route information for their events
- Views all enrolments for their events
- Captures and corrects results

**Participant**
- Registers, logs in, and views and updates their own profile
- Browses events, categories and routes
- Enters an event by choosing a category, and can cancel their own enrolment
- Views their own enrolments and results

Role rules are enforced in the API. An Organiser function returns **403** to a Participant, and a Participant function returns **403** to an Organiser. Non-public endpoints return **401** when the user is not logged in. An Organiser can only change events they own.

## Part 2 – Running the API

**Requirements:** Visual Studio 2022, the .NET 8 SDK, SQL Server Express and SSMS.

1. Clone this repository.
2. Open `RaceDay.Api/RaceDay.Api.sln` in Visual Studio.
3. In `RaceDay.Api/RaceDay.Api/appsettings.json`, check the connection string matches your SQL Server instance. The default is `localhost\SQLEXPRESS`.
4. Open **Tools → NuGet Package Manager → Package Manager Console** and run:
```
   Update-Database
```
   This creates the `RaceDayApiDb` database from the EF Core migration.
5. Press the green play button. Swagger opens at `https://localhost:<port>/swagger`.
6. In Swagger, create an account with **POST /api/auth/register**, then log in with **POST /api/auth/login**. A session cookie keeps you logged in for the following requests.

### Running the unit tests

In Visual Studio, click **Test → Run All Tests**. Or, from a terminal in the repository folder:

```
dotnet test RaceDay.Api/RaceDay.Api.sln
```

The tests use an in-memory database, so SQL Server is not needed. There are 42 tests covering registration and login, event management, role-based rejection, and enrolments and results.

### Design decisions

- **Passwords** are hashed with BCrypt and never stored in plain text.
- **Sessions** store the user's id and role. A `SessionAuthorize` filter checks them on each protected endpoint.
- **One Users table** holds both roles, separated by a `Role` column.
- **Deleting** an event or category that already has enrolments is blocked (409 Conflict).
- **Cancelling an enrolment** sets its status to `Cancelled` rather than deleting the row.

## Differences between the Part 1 plan and the code

- The `Events` table has a `DistanceKm` column in the API, because the Part 2 brief requires every event to have a distance. [Update this line once the Part 1 ERD and SQL script have been updated to match.]
- The API uses a separate database called `RaceDayApiDb`, created by EF Core migrations. The Part 1 script creates `RaceDayDB` and is kept as planning evidence.
- Cancelling an enrolment is a status change (`Cancelled`) and not a hard delete.

## /docs

- `raceday_erd.png` – Entity Relationship Diagram
- `api_endpoint_plan.md` – endpoint plan table
- `raceday_schema.sql` – SQL Server script with schema and seed data

## CI/CD

GitHub Actions builds the API, runs all unit tests, and checks the `/docs` folder on every push.

<img width="1364" height="720" alt="Testing" src="https://github.com/user-attachments/assets/8abe2f4f-c2b9-4510-9720-2cbf4b928c4b" />


## Video Walkthroughs

- Part 1: [paste unlisted YouTube link]
- Part 2: [paste unlisted YouTube link]

## AI Tool Disclosure

I used Claude (Anthropic) for planning support, drafting code and tests, and troubleshooting setup issues. I reviewed, ran and tested the work myself, and I explain it in my videos.

