# dotNet-101

A minimal **API-only** ASP.NET Core application in the *-101 family. It mirrors the JSON API contract of [fastAPI-101](https://github.com/iammikek/fastAPI-101) with JWT auth, layered services, rate limiting, and xUnit API tests — but **no server-rendered shop UI**.

## API-only by design

Like [java-101](https://github.com/iammikek/java-101), [nest-101](https://github.com/iammikek/nest-101), and [express-101](https://github.com/iammikek/express-101), this repo has **no `/shop`**. ASP.NET Core can host Razor/MVC pages, but this port stays intentionally **JSON API only**. Pair it with [react-101](https://github.com/iammikek/react-101), [vue-101](https://github.com/iammikek/vue-101), [alpine-101](https://github.com/iammikek/alpine-101), or [flutter-101](https://github.com/iammikek/flutter-101).

**Why ASP.NET Core?** Learn the same *-101 contract with C#, minimal APIs, dependency injection, and JWT — the stack many teams use for production APIs on .NET.

## What's included

- ASP.NET Core **8** minimal API on port **8010**
- Layered `src/` structure: `Api`, `Application`, `Domain`, `Infrastructure`
- JWT authentication (`sub` = email) with password hashing
- Categories + items CRUD, pagination, filters, stats summary
- Domain errors with `{ detail, code }` responses
- Rate limiting on auth (10/min) and write endpoints (60/min)
- **xUnit** API tests mirroring fastAPI-101 scenarios (plus service-layer tests)
- Swagger/OpenAPI at `/swagger`
- Dockerfile, compose.yaml, GitHub Actions CI, Makefile

> Persistence today is an **in-memory store** (fine for learning and tests). SQLite/EF Core is the natural next step for Docker/prod parity with java-101 and nest-101.

## Quick start

### Local

Requires the **.NET 8 SDK**.

```bash
cd dotNet-101
cp .env.example .env
dotnet restore
dotnet run --project src/dotNet101.Api
# or: make serve
```

Open **http://127.0.0.1:8010** — you should see:

```json
{"message":"Hello from dotNet-101"}
```

Swagger UI: **http://127.0.0.1:8010/swagger**

### Docker

```bash
docker compose -f compose.yaml up --build
```

API on **http://localhost:8010**.

### Tests

```bash
dotnet test
# or: make test
```

Docker tests:

```bash
make docker-test
```

## API endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | — | Hello message |
| GET | `/health` | — | Health check (`database: connected`) |
| POST | `/auth/register` | — | Register user |
| POST | `/auth/login` | — | Login (form `username`/`password`) |
| GET | `/auth/me` | JWT | Current user |
| GET | `/categories` | — | List categories |
| GET | `/categories/:id` | — | Show category |
| POST/PATCH/DELETE | `/categories` | JWT | Manage categories |
| GET | `/items` | — | List items (paginated, filterable) |
| GET | `/items/stats/summary` | — | Item statistics |
| GET | `/items/:id` | — | Show item |
| POST/PATCH/DELETE | `/items` | JWT | Manage items |

Write operations require `Authorization: Bearer <token>`.

Login uses OAuth2-style form fields (`username` = email), matching fastAPI-101.

Item filters: `min_price`, `max_price`, `category_id`, `name_contains`.

## Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_URLS` | `http://127.0.0.1:8010` | Bind address and port |
| `ASPNETCORE_ENVIRONMENT` | `Development` | ASP.NET Core environment |
| `JWT_SECRET` | `change-me-in-production` | JWT signing secret |
| `JWT_EXPIRE_MINUTES` | `60` | Token lifetime |
| `DATABASE_CONNECTION` | `Data Source=./data/app.db` | Reserved for upcoming SQLite/EF Core |

## Project structure

```
dotNet-101/
├── src/
│   ├── dotNet101.Api/            # HTTP entry point, endpoints, Swagger
│   ├── dotNet101.Application/    # Services + DTOs
│   ├── dotNet101.Domain/         # Entities
│   └── dotNet101.Infrastructure/ # Store, JWT, hashing, rate limit
├── tests/
│   └── dotNet101.Api.Tests/      # xUnit API + service tests
├── Dockerfile
├── compose.yaml
└── Makefile
```

## Quick reference

| Goal | Command |
|------|---------|
| Copy env | `cp .env.example .env` |
| Run local | `make serve` → http://127.0.0.1:8010 |
| Run tests | `make test` |
| Docker | `docker compose -f compose.yaml up --build` |

## *-101 Family

Full family list, ports, and clone-with-submodules: **[learning-101](https://github.com/iammikek/learning-101)**. Site catalogue: [automica.io/learning-101](https://automica.io/learning-101.html).
