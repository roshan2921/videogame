# VideoGameApi

ASP.NET Core Web API (.NET 9) for managing video games. Data is stored in a static in-memory list for learning and prototyping full CRUD without a database.

## What we built

1. **`VideoGame` model** — properties: `Id`, `Title`, `Platform`, `Developer`, `Publisher`
2. **Static seed data** — three sample games (Spider-Man 2, Zelda: Breath of the Wild, Elden Ring)
3. **CRUD endpoints** on `VideoGameController`
4. **API docs** with OpenAPI + [Scalar](https://scalar.com/) in Development

## Run the project

```bash
dotnet run
```

- HTTP: `http://localhost:5139`
- HTTPS: `https://localhost:7018`
- Scalar UI: [https://localhost:7018/scalar/v1](https://localhost:7018/scalar/v1)

## API endpoints

Base path: `/api/VideoGame`

| Method   | Endpoint             | Description                          | Success      |
|----------|----------------------|--------------------------------------|--------------|
| `GET`    | `/api/VideoGame`     | Get all video games                  | `200 OK`     |
| `GET`    | `/api/VideoGame/{id}`| Get one video game by id             | `200 OK`     |
| `POST`   | `/api/VideoGame`     | Create a video game (Id auto-assigned)| `201 Created`|
| `PUT`    | `/api/VideoGame/{id}`| Update an existing video game        | `204 No Content` |
| `DELETE` | `/api/VideoGame/{id}`| Delete a video game                  | `204 No Content` |

Missing ids return `404 Not Found`.

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

## Project layout

```
VideoGameApi/
├── Controllers/
│   └── VideoGameController.cs   # CRUD actions + static list
├── VideoGame.cs                 # Model
├── Program.cs                   # App setup, OpenAPI, Scalar
└── README.md
```

## Notes

- Changes from POST/PUT/DELETE live only in memory and reset when the app restarts.
- Scalar and OpenAPI are enabled only when `ASPNETCORE_ENVIRONMENT` is `Development`.
