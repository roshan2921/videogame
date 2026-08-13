# Concepts & Study Guide

A learning companion for this project. Use it to understand **what we built**, **why it works**, and **what to revisit** as you grow from beginner to senior developer.

Pair this with [README.md](README.md) for setup, commands, and API reference.

---
## Table of contents
1. [Big picture — how a request flows](#1-big-picture--how-a-request-flows)
2. [C# language concepts](#2-c-language-concepts)
3. [ASP.NET Core Web API](#3-aspnet-core-web-api)
4. [Entity Framework Core & SQL Server](#4-entity-framework-core--sql-server)
5. [Configuration & environments](#5-configuration--environments)
6. [API documentation (OpenAPI + Scalar)](#6-api-documentation-openapi--scalar)
7. [Project structure & tooling](#7-project-structure--tooling)
8. [HTTP & REST recap](#8-http--rest-recap)
9. [Senior developer lens](#9-senior-developer-lens)
10. [Study checklist — questions & answers](#10-study-checklist--questions--answers)
11. [Revisit later — questions & answers](#11-revisit-later--questions--answers)

---

## 1. Big picture — how a request flows

When someone calls `GET /api/VideoGame`, here is the path through our app:

```
Client (browser, Postman, .http file)
    │
    ▼
Kestrel (built-in web server)
    │
    ▼
Middleware pipeline (HTTPS redirect, authorization, etc.)
    │
    ▼
Routing → matches [Route("api/[controller]")] on VideoGameController
    │
    ▼
Controller action → GetVideoGames()
    │
    ▼
VideoGameDbContext (EF Core) → SQL Server
    │
    ▼
JSON response → 200 OK with list of VideoGame objects
```

**Key idea:** ASP.NET Core is a **pipeline**. `Program.cs` registers services (DI container) and configures middleware. Controllers are the last step — they handle HTTP and return responses.

---

## 2. C# language concepts

### Namespaces and files

```csharp
namespace VideoGameApi
{
    public class VideoGame { ... }
}
```

- A **namespace** groups related types and avoids name collisions.
- Convention: namespace matches folder structure (`VideoGameApi.Data` for `Data/VideoGameDbContext.cs`).

### Classes and properties

Our model is a simple **POCO** (Plain Old CLR Object):

```csharp
public class VideoGame
{
    public int Id { get; set; }
    public string? Title { get; set; }
    // ...
}
```

| Concept | What it means |
|---------|----------------|
| `class` | Blueprint for objects with fields/properties |
| `{ get; set; }` | Auto-property — readable and writable |
| `int` | Value type — always has a value (default `0`) |
| `string?` | Reference type — **nullable** (may be `null`) |

### Nullable reference types (`Nullable enable` in `.csproj`)

With `<Nullable>enable</Nullable>`, the compiler warns when you might use `null` unsafely. `string?` means “this can be null.” `string` means “we intend this to never be null.”

**Revisit later:** validation, required modifiers (`required string Title`), and null-forgiving operator (`!`).

### Primary constructors (C# 12)

```csharp
public class VideoGameController(VideoGameDbContext context) : ControllerBase
```

The constructor parameter `context` becomes a field automatically. Same pattern in `VideoGameDbContext`:

```csharp
public class VideoGameDbContext(DbContextOptions<VideoGameDbContext> options)
    : DbContext(options)
```

**Before C# 12:** you wrote a constructor body and assigned `context = context` manually.

### `async` / `await`

```csharp
public async Task<ActionResult<List<VideoGame>>> GetVideoGames()
{
    return Ok(await context.VideoGames.ToListAsync());
}
```

| Piece | Meaning |
|-------|---------|
| `async` | Method can use `await` and return a `Task` |
| `await` | Pause without blocking the thread until I/O completes |
| `Task<T>` | A promise of a future result |

Database calls are **I/O-bound**. Async frees the thread while SQL Server works — important under load.

**Revisit later:** `ConfigureAwait`, cancellation tokens (`CancellationToken`), and when *not* to use async.

### Pattern matching — `is null`

```csharp
if (videoGame is null)
    return NotFound();
```

Clearer than `== null` in modern C#. `is not null` is the opposite check.

### `nameof`

```csharp
return CreatedAtAction(nameof(GetVideoGameById), new { id = newGame.Id }, newGame);
```

`nameof(GetVideoGameById)` returns the string `"GetVideoGameById"` at compile time — safe if you rename the method.

### Implicit usings

`<ImplicitUsings>enable</ImplicitUsings>` in the project file imports common namespaces globally (`System`, `System.Linq`, etc.) so you don’t need `using System;` everywhere.

---

## 3. ASP.NET Core Web API

### Minimal hosting model (`Program.cs`)

.NET 6+ uses a **single `Program.cs`** instead of separate `Startup.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);  // 1. Configure services
var app = builder.Build();                         // 2. Build app
app.MapControllers();                              // 3. Configure pipeline
app.Run();                                         // 4. Start listening
```

### Dependency Injection (DI)

```csharp
builder.Services.AddDbContext<VideoGameDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

| Term | Meaning |
|------|---------|
| **Service** | Something the app needs (DbContext, logger, etc.) |
| **DI container** | Built-in registry that creates and wires services |
| `AddDbContext` | Register DbContext — scoped per HTTP request |
| Constructor injection | Controller receives `VideoGameDbContext` automatically |

**Lifetime cheat sheet:**

| Registration | Lifetime |
|--------------|----------|
| `AddSingleton` | One instance for entire app |
| `AddScoped` | One instance per HTTP request (DbContext default) |
| `AddTransient` | New instance every time it’s requested |

### Controllers

```csharp
[Route("api/[controller]")]
[ApiController]
public class VideoGameController(VideoGameDbContext context) : ControllerBase
```

| Attribute | Role |
|-----------|------|
| `[ApiController]` | API behaviors — automatic model validation, binding rules |
| `[Route("api/[controller]")]` | Base URL → `/api/VideoGame` |
| `[HttpGet]`, `[HttpPost]`, etc. | Maps HTTP method to action |
| `[HttpGet("{id}")]` | Route parameter → `int id` |

`ControllerBase` is for APIs (no view support). MVC controllers inherit `Controller`.

### Return types and status codes

| Return | HTTP | When |
|--------|------|------|
| `Ok(data)` | 200 | Success with body |
| `CreatedAtAction(...)` | 201 | Resource created + location |
| `NoContent()` | 204 | Success, no body (update/delete) |
| `NotFound()` | 404 | Resource missing |
| `BadRequest()` | 400 | Invalid input |

`ActionResult<T>` lets you return either `T` or an error result in one signature.

### Model binding

On `POST`, JSON body:

```json
{ "title": "God of War", "platform": "PS5" }
```

ASP.NET Core deserializes JSON into a `VideoGame` parameter automatically. Property names match case-insensitively by default (`title` → `Title`).

**Revisit later:** DTOs (separate request/response models), `[FromBody]`, `[FromRoute]`, custom binders.

### Middleware pipeline

```csharp
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
```

Each middleware can process or short-circuit the request. Order matters.

**Revisit later:** custom middleware, exception handling middleware, CORS.

---

## 4. Entity Framework Core & SQL Server

### What is EF Core?

**Entity Framework Core** is an **ORM** (Object-Relational Mapper). You work with C# classes; EF translates to SQL.

### DbContext

```csharp
public class VideoGameDbContext(DbContextOptions<VideoGameDbContext> options)
    : DbContext(options)
{
    public DbSet<VideoGame> VideoGames => Set<VideoGame>();
}
```

| Piece | Role |
|-------|------|
| `DbContext` | Session with the database — tracks changes |
| `DbSet<VideoGame>` | Represents the `VideoGames` table |
| `Set<VideoGame>()` | Factory for the DbSet |

### Code-first workflow

1. Define C# model (`VideoGame`)
2. Define `DbContext`
3. `Add-Migration Initial` → generates migration C# files
4. `Update-Database` → applies migration to SQL Server

The **database schema is derived from your code**, not designed in SSMS first.

### Migrations

Migrations are versioned schema changes stored in `Migrations/`. They should live in source control.

- **Up** — apply changes (`Update-Database`)
- **Down** — revert (`Update-Database -Migration PreviousName`)

Never delete the `Migrations` folder after generating — teammates and CI need it.

### `OnModelCreating` and seed data

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
  modelBuilder.Entity<VideoGame>().HasData(
      new VideoGame { Id = 1, Title = "Spider-Man 2", ... }
  );
}
```

`HasData` inserts rows when the migration runs. **Changing seed data requires a new migration.**

### Common EF operations in our controller

| Code | SQL-ish meaning |
|------|-----------------|
| `context.VideoGames.ToListAsync()` | `SELECT * FROM VideoGames` |
| `context.VideoGames.FindAsync(id)` | `SELECT * WHERE Id = @id` |
| `context.VideoGames.Add(newGame)` | `INSERT` (on SaveChanges) |
| `context.VideoGames.Remove(game)` | `DELETE` |
| `await context.SaveChangesAsync()` | Commit pending changes |

### Change tracking

When you `FindAsync` an entity, EF **tracks** it. Assigning `videoGame.Title = ...` marks the entity modified; `SaveChangesAsync` generates `UPDATE`.

### Connection string

```
Server=localhost\SQLExpress;Database=VideoGameDb;Trusted_Connection=true;TrustServerCertificate=true;
```

| Part | Meaning |
|------|---------|
| `Server` | SQL Server instance |
| `Database` | Database name |
| `Trusted_Connection=true` | Windows authentication |
| `TrustServerCertificate=true` | Skip cert validation (common in local dev) |

**Revisit later:** connection pooling, retry policies, read replicas, Azure SQL.

---

## 5. Configuration & environments

### `appsettings.json`

Hierarchical JSON loaded automatically. Connection strings and logging levels live here.

### `appsettings.Development.json`

Overrides base settings when `ASPNETCORE_ENVIRONMENT=Development` (set in `launchSettings.json`).

### Accessing config in code

```csharp
builder.Configuration.GetConnectionString("DefaultConnection")
```

**Revisit later:** User Secrets (`dotnet user-secrets`), environment variables, Azure Key Vault — never commit production secrets.

---

## 6. API documentation (OpenAPI + Scalar)

```csharp
builder.Services.AddOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();           // /openapi/v1.json
    app.MapScalarApiReference(); // /scalar/v1 — interactive UI
}
```

| Tool | Purpose |
|------|---------|
| **OpenAPI** | Machine-readable API spec (formerly Swagger) |
| **Scalar** | Modern UI to browse and try endpoints |

Gated to **Development** so production doesn’t expose internal API docs by default.

---

## 7. Project structure & tooling

```
VideoGameApi/
├── Controllers/          # HTTP endpoints
├── Data/                 # DbContext, future repositories
├── Migrations/           # EF migration history
├── VideoGame.cs          # Domain model
├── Program.cs            # App entry + DI + pipeline
├── appsettings.json      # Configuration
├── VideoGameApi.http     # IDE HTTP client requests
├── VideoGameApi.csproj   # Project definition + NuGet packages
└── VideoGameApi.sln      # Solution wrapper
```

### Key NuGet packages

| Package | Purpose |
|---------|---------|
| `Microsoft.EntityFrameworkCore` | EF Core runtime |
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server provider |
| `Microsoft.AspNetCore.OpenApi` | OpenAPI generation |
| `Scalar.AspNetCore` | API docs UI |

### `VideoGameApi.http`

Rider/Visual Studio HTTP client file — run requests without Postman. Variables like `{{VideoGameApi_HostAddress}}` keep URLs reusable.

### `.gitignore`

Excludes `bin/`, `obj/`, IDE folders — build output should not be committed.

---

## 8. HTTP & REST recap

Our API follows **REST** conventions:

| Method | Path | Action | Idempotent? |
|--------|------|--------|-------------|
| GET | `/api/VideoGame` | List all | Yes |
| GET | `/api/VideoGame/{id}` | Get one | Yes |
| POST | `/api/VideoGame` | Create | No |
| PUT | `/api/VideoGame/{id}` | Full update | Yes |
| DELETE | `/api/VideoGame/{id}` | Delete | Yes |

**Idempotent** — calling twice has the same effect as once (POST is not — two POSTs create two rows).

---

## 9. Senior developer lens

What we have is a solid **learning baseline**. What production APIs typically add:

### Architecture

| Topic | Current state | Typical evolution |
|-------|---------------|-------------------|
| **DTOs** | Model used for API + DB | Separate `CreateVideoGameDto`, `VideoGameResponse` |
| **Validation** | Minimal (`is null`) | FluentValidation or Data Annotations |
| **Repository pattern** | Controller uses DbContext directly | `IVideoGameRepository` for testability |
| **Service layer** | None | Business logic out of controllers |
| **Error handling** | Per-action `NotFound()` | Global exception filter / `ProblemDetails` |
| **Logging** | Default only | Structured logging (Serilog) |
| **Testing** | None yet | Unit + integration tests with `WebApplicationFactory` |

### Security

- No authentication/authorization yet (`UseAuthorization` is registered but unused).
- No input sanitization beyond null checks.
- `TrustServerCertificate=true` is fine locally; review for production.

### Data integrity

- Client can set `Id` on POST — EF may conflict with identity column.
- No concurrency tokens (`RowVersion`) for concurrent updates.
- No soft delete or audit fields (`CreatedAt`, `UpdatedBy`).

### Performance

- `ToListAsync()` loads entire table — fine for small data; paginate for scale (`Skip`/`Take`).
- No caching, no compiled queries, no `AsNoTracking()` for read-only queries.

### API design

- `PUT` expects full resource — `PATCH` for partial updates is common at scale.
- No HATEOAS, versioning (`/api/v1/...`), or rate limiting.

**The senior mindset:** our code is correct and teachable. Production hardening is layered on intentionally — understand each layer before adding it.

---

## 10. Study checklist — questions & answers

Use this when revisiting. Read each question, try to answer from memory, then check yourself against the answer below.

### C# fundamentals

#### Q1. Explain `async`/`await` and why DB calls use it

**Answer:** `async` marks a method that can perform work without blocking the thread that called it. Inside an `async` method, `await` pauses *that method* until an operation finishes (like a database query), but the thread is returned to the pool so it can handle other requests.

In our controller:

```csharp
return Ok(await context.VideoGames.ToListAsync());
```

`ToListAsync()` talks to SQL Server over the network — that is **I/O-bound** work (waiting, not computing). Without async, the thread would sit idle during the wait. With async, ASP.NET Core can serve other requests on that thread while SQL Server responds.

`Task<T>` is the return type: a handle for work that will complete later. `Task<ActionResult<List<VideoGame>>>` means “eventually you’ll get either a list result or an error response.”

---

#### Q2. Explain nullable reference types (`string?`)

**Answer:** With `<Nullable>enable</Nullable>` in `VideoGameApi.csproj`, the compiler tracks whether reference types (like `string`) can be null.

- `string? Title` — this property **may** be `null`. The compiler won’t warn if you assign `null`, but it will warn if you use `Title` without checking for null first.
- `string Title` (no `?`) — you’re telling the compiler “I intend this to never be null.” Assigning `null` may produce a warning.

Value types use a different system: `int` cannot be null; `int?` (or `Nullable<int>`) can.

In our model, `Title`, `Platform`, etc. are `string?` because we haven’t added validation yet — a client could omit them or send null. **Revisit:** add `required string Title` or validation so empty titles are rejected at the API layer.

---

#### Q3. Explain primary constructors

**Answer:** Primary constructors (C# 12) let you declare constructor parameters directly on the class declaration. The compiler creates a private field for each parameter.

**Our code:**

```csharp
public class VideoGameController(VideoGameDbContext context) : ControllerBase
```

`context` is available everywhere in the class — no need to write:

```csharp
private readonly VideoGameDbContext _context;
public VideoGameController(VideoGameDbContext context) { _context = context; }
```

Same pattern in `VideoGameDbContext(DbContextOptions<VideoGameDbContext> options) : DbContext(options)` — the `options` are passed to the base `DbContext` constructor.

**Why it matters:** less boilerplate, and DI (Dependency Injection) still works — ASP.NET Core sees the constructor parameter type (`VideoGameDbContext`) and supplies the registered instance automatically.

---

#### Q4. Explain `Task<ActionResult<T>>` vs `IActionResult`

**Answer:**

| Return type | Used in our project | Meaning |
|-------------|---------------------|---------|
| `Task<ActionResult<VideoGame>>` | `GetVideoGameById`, `CreateVideoGame` | Async method that returns a `VideoGame` on success **or** an error result (`NotFound`, `BadRequest`) |
| `Task<IActionResult>` | `UpdateVideoGame`, `DeleteVideoGame` | Async method that returns any action result, with **no** typed success payload |

`ActionResult<T>` is a union: success gives you `T` in the body; failure gives status codes without `T`. OpenAPI can infer the success response type from `ActionResult<VideoGame>`.

`IActionResult` is the base interface for all results (`Ok`, `NotFound`, `NoContent`, etc.). Use it when there’s no single success type or no body on success — our `PUT` and `DELETE` return `204 No Content`, so `IActionResult` is appropriate.

Both are wrapped in `Task<>` because the methods are `async`.

---

### ASP.NET Core

#### Q5. Trace a request from Kestrel to controller

**Answer:** Step-by-step for `GET https://localhost:7018/api/VideoGame`:

1. **Kestrel** — ASP.NET Core’s built-in web server receives the HTTP request on port 7018.
2. **Host** — `WebApplication` (`Program.cs`) forwards the request into the middleware pipeline.
3. **`UseHttpsRedirection`** — if you hit HTTP, you’d be redirected to HTTPS (we’re already on HTTPS here).
4. **`UseAuthorization`** — authorization middleware runs (we have no auth policies yet, so nothing blocks the request).
5. **Endpoint routing** — routing matches URL `/api/VideoGame` + HTTP method `GET` to `VideoGameController.GetVideoGames` because of `[Route("api/[controller]")]` and `[HttpGet]`.
6. **DI** — before the action runs, ASP.NET Core creates a **scoped** `VideoGameDbContext` (registered in `Program.cs`) and passes it into the controller’s primary constructor.
7. **Action executes** — `await context.VideoGames.ToListAsync()` queries SQL Server.
8. **Result** — `Ok(...)` serializes the list to JSON and returns `200 OK`.
9. **Response** — travels back through middleware to Kestrel, then to the client.

If routing finds no match, you get `404` before any controller runs.

---

#### Q6. Explain DI and why DbContext is injected

**Answer:** **Dependency Injection (DI)** means objects don’t create their own dependencies — they receive them from the **DI container** (`builder.Services` in `Program.cs`).

Registration:

```csharp
builder.Services.AddDbContext<VideoGameDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

This tells the container: “When something needs `VideoGameDbContext`, create one configured for SQL Server.”

Consumption:

```csharp
public class VideoGameController(VideoGameDbContext context) : ControllerBase
```

The framework resolves `context` automatically — you never write `new VideoGameDbContext(...)`.

**Why inject DbContext?**

- **Configuration** — connection string lives in config, not hard-coded in the controller.
- **Lifetime** — DbContext is **scoped** (one per HTTP request), which matches EF’s design (track changes for one request, then dispose).
- **Testability** — in tests you can register a fake or in-memory DbContext instead of SQL Server.
- **Separation of concerns** — the controller handles HTTP; it doesn’t own database setup.

---

#### Q7. Explain difference between `Controller` and `ControllerBase`

**Answer:**

| Base class | Purpose | Extra features |
|------------|---------|----------------|
| `ControllerBase` | **API controllers** (our `VideoGameController`) | HTTP helpers: `Ok()`, `NotFound()`, `BadRequest()`, etc. No view support. |
| `Controller` | **MVC controllers** (HTML pages) | Everything in `ControllerBase` **plus** view-related methods like `View()`, `ViewData`, etc. |

Our project is a **Web API** — we return JSON, not Razor pages. Inheriting `Controller` would add unused view APIs.

`WeatherForecastController` in the template also uses `ControllerBase` for the same reason.

Rule of thumb: JSON API → `ControllerBase`; server-rendered HTML → `Controller`.

---

#### Q8. Name all HTTP status codes we return and why

**Answer:**

| Status | Method(s) | When we return it | Why |
|--------|-----------|-------------------|-----|
| **200 OK** | `GET` (all, by id) | Data found | Standard success with a JSON body |
| **201 Created** | `POST` | New game saved | REST convention: resource was created; `CreatedAtAction` also points to `GET /api/VideoGame/{id}` |
| **204 No Content** | `PUT`, `DELETE` | Update/delete succeeded | Success but no body needed — client already knows the id |
| **400 Bad Request** | `POST` | `newGame is null` | Client sent invalid/unbindable input |
| **404 Not Found** | `GET` by id, `PUT`, `DELETE` | No row with that `id` | Resource doesn’t exist; don’t pretend success |

We do **not** currently return: `401 Unauthorized`, `403 Forbidden`, `409 Conflict`, or `500` explicitly — unhandled exceptions become `500` from the framework.

---

### EF Core

#### Q9. Explain code-first vs database-first

**Answer:**

| Approach | You start with… | EF generates… | Our project |
|----------|-----------------|---------------|-------------|
| **Code-first** | C# classes (`VideoGame`, `DbContext`) | SQL schema via **migrations** | ✅ This one |
| **Database-first** | Existing SQL database | C# entities (often scaffolded) | ❌ Not used |

**Code-first flow we use:**

1. Write `VideoGame` model.
2. Write `VideoGameDbContext` with `DbSet<VideoGame>`.
3. `Add-Migration Initial` — EF compares models to the database (empty) and generates migration code.
4. `Update-Database` — EF runs migration SQL against SQL Server → creates `VideoGames` table.

When you change the model (add a property), you add another migration and update again. The **source of truth is C# code**, not the SSMS designer.

---

#### Q10. Walk through `Add-Migration` → `Update-Database`

**Answer:**

**`Add-Migration Initial`** (or `dotnet ef migrations add Initial`):

1. EF Core scans `VideoGameDbContext` and entity classes.
2. Compares that model to the last migration snapshot (or empty if first migration).
3. Creates files in `Migrations/`:
   - `YYYYMMDDHHMMSS_Initial.cs` — `Up()` and `Down()` methods with schema changes.
   - `VideoGameDbContextModelSnapshot.cs` — current model state for future diffs.

Nothing touches the database yet — this only writes C# files.

**`Update-Database`** (or `dotnet ef database update`):

1. EF looks at applied migrations in the database (`__EFMigrationsHistory` table).
2. Runs any pending `Up()` methods in order.
3. For `Initial`: creates database `VideoGameDb` (if missing), creates `VideoGames` table with columns matching `VideoGame`, applies `HasData` seed inserts.

**After changing seed data in `OnModelCreating`:**

```powershell
Add-Migration SeedData
Update-Database
```

A new migration captures the seed change; `Update-Database` applies it. Existing rows are not always re-seeded automatically — EF migrations handle inserts/updates per migration design; for complex seed changes you may need SQL scripts or `dotnet ef migrations remove` in dev.

---

#### Q11. Explain what `SaveChangesAsync` does

**Answer:** EF Core **tracks** entities attached to the `DbContext`. Operations like `Add`, `Remove`, and property changes on tracked entities are queued in memory — **no SQL runs yet**.

`await context.SaveChangesAsync()`:

1. Builds SQL for all pending changes (`INSERT`, `UPDATE`, `DELETE`).
2. Sends them to SQL Server in a transaction (by default).
3. For inserts with identity columns, reads generated `Id` back into the entity (so `newGame.Id` is populated after create).
4. Clears the change tracker for those entities (they’re now synced with the DB).

**In our code:**

- **POST:** `Add(newGame)` → `SaveChangesAsync()` → INSERT.
- **PUT:** modify tracked entity properties → `SaveChangesAsync()` → UPDATE.
- **DELETE:** `Remove(videoGame)` → `SaveChangesAsync()` → DELETE.

If `SaveChangesAsync` fails (constraint violation, connection error), an exception is thrown and nothing is committed.

**GET** actions don’t call `SaveChangesAsync` — read-only queries don’t need it.

---

#### Q12. Explain `HasData` and when you need a new migration

**Answer:** `HasData` in `OnModelCreating` tells EF Core: “these rows should exist in the table when migrations are applied.”

```csharp
modelBuilder.Entity<VideoGame>().HasData(
    new VideoGame { Id = 1, Title = "Spider-Man 2", ... },
    // ...
);
```

**Requirements for seed data:**

- Primary key (`Id`) must be specified — EF needs to know which row is which in future migrations.
- All required columns must have values.

**When you need a new migration:**

- You add, change, or remove seed rows in `HasData`.
- You change the entity model (new property, renamed column, new table).
- You change relationships, indexes, or constraints in `OnModelCreating`.

**When you don’t:**

- You insert data at runtime in the controller (that’s normal CRUD, not seeding).
- You only change controller logic without schema changes.

After editing `HasData`, always `Add-Migration <Name>` then `Update-Database` so the database and snapshot stay aligned.

---

### Configuration & ops

#### Q13. Read and explain each part of the connection string

**Answer:** Our connection string:

```
Server=localhost\SQLExpress;Database=VideoGameDb;Trusted_Connection=true;TrustServerCertificate=true;
```

| Part | Value | Meaning |
|------|-------|---------|
| `Server` | `localhost\SQLExpress` | SQL Server instance. `localhost` = this machine. `\SQLExpress` = named instance (SQL Server Express default). |
| `Database` | `VideoGameDb` | Database name EF connects to. Created on first `Update-Database` if it doesn’t exist. |
| `Trusted_Connection` | `true` | Use **Windows authentication** (your Windows login) instead of SQL username/password. |
| `TrustServerCertificate` | `true` | Don’t validate the SSL certificate strictly. Common for local dev with self-signed certs. **Review for production.** |

Optional parts we don’t use yet: `User Id` / `Password` (SQL auth), `MultipleActiveResultSets=true`, timeout settings, encryption flags.

Read from config via `builder.Configuration.GetConnectionString("DefaultConnection")` — maps to `"ConnectionStrings:DefaultConnection"` in `appsettings.json`.

---

#### Q14. Explain Development vs Production environment

**Answer:** `ASPNETCORE_ENVIRONMENT` tells the app which environment it’s running in. Set to `Development` in `Properties/launchSettings.json` when you run locally.

| Aspect | Development | Production |
|--------|-------------|------------|
| **Config files** | `appsettings.json` + `appsettings.Development.json` (overrides) | `appsettings.json` + `appsettings.Production.json` |
| **Scalar / OpenAPI** | Enabled (`if (app.Environment.IsDevelopment())`) | Disabled in our app — docs not exposed |
| **Detailed errors** | More verbose exception pages (if enabled) | Generic errors to clients; details in logs |
| **Secrets** | User Secrets, local connection strings | Environment variables, Key Vault — never commit secrets |
| **HTTPS** | Local dev certificate | Real TLS certificate |

`app.Environment.IsDevelopment()` is how we branch behavior without separate codebases.

Production hosting (IIS, Azure, Docker) sets `ASPNETCORE_ENVIRONMENT=Production` — you don’t use `launchSettings.json` on the server.

---

#### Q15. Run the API and test all CRUD via Scalar or `.http` file

**Answer:** **Steps:**

1. Ensure SQL Server Express is running and migrations applied (`Update-Database`).
2. Run `dotnet run` from the project folder.
3. Note URLs from output or `launchSettings.json`: HTTP `http://localhost:5139`, HTTPS `https://localhost:7018`.

**Option A — Scalar (browser):**

1. Open `https://localhost:7018/scalar/v1`
2. Expand `VideoGame` endpoints.
3. Try: `GET` all → `GET` by id → `POST` new game → `PUT` update → `DELETE`.

**Option B — `VideoGameApi.http` (VS / Rider):**

1. Open `VideoGameApi.http`.
2. Click “Run” on each request block (uses `@VideoGameApi_HostAddress = http://localhost:5139`).
3. Verify status codes: 200, 201, 204, 404 as expected.

**What to verify:**

- After `POST`, `GET` shows the new row and `Id` is auto-generated.
- After `DELETE`, `GET` by that id returns 404.
- Restart the API — data persists (proves SQL Server, not in-memory).

---

### Next steps — how to implement (when ready)

#### Q16. How do I add FluentValidation on create/update?

**Answer:**

1. Install: `dotnet add package FluentValidation.AspNetCore`
2. Create validators, e.g. `CreateVideoGameValidator : AbstractValidator<VideoGame>` with rules like `RuleFor(x => x.Title).NotEmpty().MaximumLength(200)`.
3. Register in `Program.cs`: `builder.Services.AddValidatorsFromAssemblyContaining<CreateVideoGameValidator>()` and enable auto-validation.
4. Invalid requests automatically return `400` with error details — no manual `if (string.IsNullOrEmpty(...))` in every action.

**Why:** Centralizes validation rules, testable without HTTP, consistent error format.

---

#### Q17. How do I introduce DTOs and mapping?

**Answer:**

1. Create separate types, e.g. `CreateVideoGameDto` (no `Id`), `UpdateVideoGameDto`, `VideoGameResponse`.
2. Controller accepts DTOs instead of `VideoGame` entity — prevents clients from setting `Id` on create or over-posting fields.
3. Map DTO → entity manually or with AutoMapper (`dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection`).
4. Never expose EF entities directly if they have internal fields you don’t want on the wire.

**Example manual map on POST:**

```csharp
var game = new VideoGame { Title = dto.Title, Platform = dto.Platform, ... };
context.VideoGames.Add(game);
```

---

#### Q18. How do I add `dotnet test` integration tests?

**Answer:**

1. Add xUnit test project: `dotnet new xunit -n VideoGameApi.Tests`
2. Reference main project; add `Microsoft.AspNetCore.Mvc.Testing`.
3. Use `WebApplicationFactory<Program>` to spin up the API in memory.
4. Replace `DbContext` with `UseInMemoryDatabase` or Testcontainers SQL in test `ConfigureWebHost`.
5. `HttpClient` from factory calls `/api/VideoGame` — assert status codes and JSON.

**Why:** Proves routing, DI, EF, and serialization work together without manual browser testing.

---

#### Q19. How do I add pagination to `GET /api/VideoGame`?

**Answer:**

1. Add query parameters: `int page = 1`, `int pageSize = 10`.
2. Query:

```csharp
var games = await context.VideoGames
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
var total = await context.VideoGames.CountAsync();
```

3. Return wrapper: `{ items, page, pageSize, totalCount }` or headers like `X-Total-Count`.

**Why:** `ToListAsync()` without pagination loads every row — breaks down with large tables.

---

#### Q20. How do I add JWT authentication?

**Answer:**

1. Install `Microsoft.AspNetCore.Authentication.JwtBearer`.
2. Configure in `Program.cs`: `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` with issuer, audience, signing key from config.
3. `builder.Services.AddAuthorization()` and policies as needed.
4. Protect endpoints: `[Authorize]` on controller or actions.
5. Clients send `Authorization: Bearer <token>` header.

**Our app today:** `UseAuthorization()` runs but nothing requires auth — all endpoints are anonymous. JWT would restrict who can create/update/delete games.

---

## 11. Revisit later — questions & answers

These topics are mentioned earlier as “revisit later.” Each question and answer is here so you don’t have to hunt through the doc.

#### Q21. What are `required`, validation, and the null-forgiving operator (`!`)?

**Answer:**

- **`required`** (C# 11) — marks a property that must be set when the object is created:

  ```csharp
  public required string Title { get; set; }
  ```

  Compiler warns if you `new VideoGame()` without `Title`. Works with JSON deserialization when the client omits the property.

- **Validation** — rules beyond nullability: “Title max 200 chars”, “Platform must be PS5/Xbox/PC”. Use Data Annotations (`[Required]`, `[MaxLength(200)]`) or FluentValidation (see Q16).

- **Null-forgiving `!`** — tells the compiler “I know this isn’t null”:

  ```csharp
  videoGame.Title!.Trim();  // suppresses warning if Title is string?
  ```

  Use sparingly — if you’re wrong, you get `NullReferenceException` at runtime.

---

#### Q22. What is `ConfigureAwait`, `CancellationToken`, and when should I *not* use async?

**Answer:**

- **`ConfigureAwait(false)`** — in library code, `await foo.ConfigureAwait(false)` avoids resuming on the original synchronization context. In ASP.NET Core there’s no classic `HttpContext` sync context, so it’s rarely needed in controllers — default `await` is fine.

- **`CancellationToken`** — passed from `HttpContext.RequestAborted` into EF calls:

  ```csharp
  await context.VideoGames.ToListAsync(cancellationToken);
  ```

  If the client disconnects, the query can cancel instead of wasting DB resources.

- **When not to use async** — CPU-bound work that doesn’t await anything; trivial synchronous methods; code that only does `return Ok(staticData)`. Don’t `async`/`await` without an await — use sync methods instead. Don’t use async to “make things faster” — it helps **scalability** under concurrent I/O, not single-request CPU speed.

---

#### Q23. What are DTOs, `[FromBody]`, `[FromRoute]`, and custom binders?

**Answer:**

- **DTO (Data Transfer Object)** — a type shaped for the API contract, separate from the database entity. Stops over-posting (client sending extra fields) and hides internal columns.

- **`[FromBody]`** — explicitly bind from JSON body (POST/PUT). Often optional in APIs because complex types default to body binding.

- **`[FromRoute]`** — bind from URL segment, e.g. `[HttpGet("{id}")]` with `[FromRoute] int id`.

- **`[FromQuery]`** — bind from query string, e.g. `?page=2&pageSize=10`.

- **Custom binders** — implement `IModelBinder` for unusual binding (e.g. parse a composite id format). Rare — default binding covers most REST APIs.

---

#### Q24. What is custom middleware, exception middleware, and CORS?

**Answer:**

- **Custom middleware** — a component in the pipeline:

  ```csharp
  app.Use(async (context, next) => { /* before */ await next(); /* after */ });
  ```

  Use for logging request timing, adding headers, etc. Order in `Program.cs` matters.

- **Exception handling middleware** — `app.UseExceptionHandler()` or `UseDeveloperExceptionPage()` in Development. Catches unhandled exceptions and returns consistent `ProblemDetails` JSON instead of crashing the connection.

- **CORS (Cross-Origin Resource Sharing)** — browser security: a web page on `https://myapp.com` can’t call your API on `https://api.other.com` unless the API sends `Access-Control-Allow-Origin`. Register `AddCors()` and `UseCors()` when you build a separate frontend (React, Angular) on a different origin.

Our API has no CORS policy yet — fine for same-origin or tools like Postman (they’re not browsers enforcing CORS).

---

#### Q25. What are connection pooling, retry policies, read replicas, and Azure SQL?

**Answer:**

- **Connection pooling** — ADO.NET reuses open SQL connections instead of creating a new TCP connection per request. Enabled by default. Don’t `new SqlConnection` manually in controllers — DbContext manages this.

- **Retry policies** — transient failures (network blip) can retry automatically:

  ```csharp
  options.UseSqlServer(connectionString, o => o.EnableRetryOnFailure());
  ```

  Useful in cloud environments.

- **Read replicas** — secondary databases for read-only queries to scale read load. EF can route queries to replicas with advanced configuration — not needed for our small app.

- **Azure SQL** — managed SQL Server in Azure. Connection string changes to Azure hostname + SQL authentication or managed identity. Same EF Core code; different connection string and firewall rules.

---

#### Q26. How do User Secrets, environment variables, and Key Vault fit in?

**Answer:**

| Mechanism | When to use |
|-----------|-------------|
| **`appsettings.json`** | Non-secret defaults (logging levels, feature flags) |
| **User Secrets** (`dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."`) | Local dev secrets — stored outside the repo in a user profile folder |
| **Environment variables** | Production/Docker/Kubernetes — `ConnectionStrings__DefaultConnection` (double underscore = nested JSON key) |
| **Azure Key Vault** | Production secrets with rotation, audit, and access policies |

**Rule:** never commit passwords, API keys, or production connection strings to git. Our repo uses Windows auth locally — no password in the string, which is fine for learning.

---

## Quick reference — files to read in order

1. `VideoGame.cs` — simplest type, start here
2. `Data/VideoGameDbContext.cs` — database mapping + seeds
3. `Controllers/VideoGameController.cs` — HTTP + EF usage
4. `Program.cs` — how everything connects
5. `appsettings.json` — configuration
6. `README.md` — commands and API table

---

*Last updated to match the project: ASP.NET Core 9, EF Core 9, SQL Server, Scalar OpenAPI docs.*
