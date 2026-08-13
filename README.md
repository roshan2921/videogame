# VideoGameApi

ASP.NET Core Web API (.NET 9) for managing video games. Data is persisted in **SQL Server** with **Entity Framework Core** (code-first migrations and seed data).

**Learning guide:** [CONCEPTS.md](CONCEPTS.md) — explanations of every C#, .NET, and EF Core concept used in this project, with a senior-developer revisit section and study checklist.

## What we built

1. **`VideoGame` model** — `Id`, `Title`, `Platform`, `Developer`, `Publisher`
2. **Entity Framework Core + SQL Server** — `VideoGameDbContext` registered in `Program.cs`
3. **Code-first migrations** — create/update the `VideoGameDb` database from the model
4. **Seed data** — sample games configured with `HasData` in `OnModelCreating`
5. **CRUD endpoints** — controller uses EF Core against the database (not an in-memory list)
6. **API docs** — OpenAPI + [Scalar](https://scalar.com/) in Development
7. **`.gitignore`** — standard ignores for .NET build/IDE artifacts

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server Express (local instance: `localhost\SQLExpress`)
- EF Core tools (Package Manager Console in Visual Studio, or `dotnet ef`)

## Configuration

Connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLExpress;Database=VideoGameDb;Trusted_Connection=true;TrustServerCertificate=true;"
}
```

## Database migrations

From **Package Manager Console** (default project: this API):

```powershell
Add-Migration Initial
Update-Database

# After adding/changing HasData seed configuration:
Add-Migration SeedData
Update-Database
```

Or with the .NET CLI (requires `dotnet-ef`):

```bash
dotnet ef migrations add Initial
dotnet ef database update
```

### Verify data in SQL

```powershell
sqlcmd -S localhost\SQLExpress -E -d VideoGameDb -Q "SELECT Id, Title, Platform FROM VideoGames"
```

Or in **SSMS**: connect to `localhost\SQLExpress` → database `VideoGameDb` → table `dbo.VideoGames` → **Select Top 1000 Rows**.

## Run the project

```bash
dotnet run
```

- HTTP: `http://localhost:5139`
- HTTPS: `https://localhost:7018`
- Scalar UI: [https://localhost:7018/scalar/v1](https://localhost:7018/scalar/v1)

## API endpoints

Base path: `/api/VideoGame`

| Method   | Endpoint              | Description                            | Success          |
|----------|-----------------------|----------------------------------------|------------------|
| `GET`    | `/api/VideoGame`      | Get all video games                    | `200 OK`         |
| `GET`    | `/api/VideoGame/{id}` | Get one video game by id               | `200 OK`         |
| `POST`   | `/api/VideoGame`      | Create a video game (`Id` from SQL identity) | `201 Created` |
| `PUT`    | `/api/VideoGame/{id}` | Update an existing video game          | `204 No Content` |
| `DELETE` | `/api/VideoGame/{id}` | Delete a video game                    | `204 No Content` |

Missing ids return `404 Not Found`. Changes are saved to SQL Server and persist across restarts.

### Example — create

`POST /api/VideoGame`

```json
{
  "title": "God of War Ragnarök",
  "platform": "PS5",
  "developer": "Santa Monica Studio",
  "publisher": "Sony Interactive Entertainment"
}
```

### Example — update

`PUT /api/VideoGame/1`

```json
{
  "title": "Spider-Man 2 Remastered",
  "platform": "PS5",
  "developer": "Insomniac Games",
  "publisher": "Sony Interactive Entertainment"
}
```

You can also try the requests in `VideoGameApi.http`.

## Project layout

```
VideoGameApi/
├── Controllers/
│   └── VideoGameController.cs   # EF Core CRUD actions
├── Data/
│   └── VideoGameDbContext.cs    # DbContext + HasData seeding
├── Migrations/                  # EF migration files (keep in source control)
├── VideoGame.cs                 # Model
├── Program.cs                   # DI, DbContext, OpenAPI, Scalar
├── appsettings.json             # SQL Server connection string
├── VideoGameApi.http            # Sample HTTP requests
├── .gitignore
├── README.md
└── CONCEPTS.md                 # Study guide for concepts used in this project
```

## Notes

- CRUD uses `VideoGameDbContext` with async EF Core APIs (`ToListAsync`, `FindAsync`, `SaveChangesAsync`).
- Seed data lives in `VideoGameDbContext.OnModelCreating` via `HasData`. Add a new migration after changing seeds, then run `Update-Database`.
- Do not delete the `Migrations` folder after generating migrations.
- Scalar and OpenAPI are enabled only when `ASPNETCORE_ENVIRONMENT` is `Development`.
