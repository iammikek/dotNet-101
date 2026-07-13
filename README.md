# dotNet-101

A minimal **API-only** ASP.NET Core application in the *-101 family. It mirrors the shared backend pattern used across the other API-focused repos: a layered structure, a service-oriented split, Docker support, and the same core endpoint shape for auth, categories, items, and stats.

## API-only by design

Other *-101 projects like Django, Laravel, and Symfony include a `/shop` browser UI because those stacks are being taught as full-stack monoliths. `dotNet-101` starts as **JSON API only** because ASP.NET Core is often used as a backend for SPAs, mobile apps, and other services.

If you later want a Razor Pages or MVC version, that is better as a separate teaching variant than mixed into the base repo.

## What's included

- ASP.NET Core minimal API on port **8010**
- Layered `src/` structure: `Api`, `Application`, `Domain`, `Infrastructure`
- Swagger/OpenAPI for endpoint discovery
- FastAPI-parity endpoints for auth, categories, items, filters, pagination, and stats
- xUnit API test project with Docker test runner support
- `Dockerfile`, `compose.yaml`, `.env.example`, and `Makefile`

## Quick start

### Local

Requires the **.NET 8 SDK**.

```bash
cd dotNet-101
cp .env.example .env
dotnet restore
dotnet run --project src/dotNet101.Api
```

Open:

- `http://127.0.0.1:8010` - hello endpoint
- `http://127.0.0.1:8010/swagger` - Swagger UI

### Docker

```bash
docker compose -f compose.yaml up --build
```

API runs on `http://localhost:8010`.

### Tests

```bash
dotnet test
```

### Docker tests

```bash
docker compose run --rm test
# or: make docker-test
```

## Project structure

```text
dotNet-101/
├── src/
│   ├── dotNet101.Api/            # HTTP entry point, endpoints, Swagger
│   ├── dotNet101.Application/    # Service contracts and use-case layer
│   ├── dotNet101.Domain/         # Core entities
│   └── dotNet101.Infrastructure/ # Cross-cutting implementations
├── tests/
│   └── dotNet101.Api.Tests/      # API parity tests
├── dotNet-101.sln
├── Directory.Build.props
├── Dockerfile
├── compose.yaml
├── Makefile
├── .env.example
└── README.md
```

## API endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | - | Hello message |
| GET | `/health` | - | Health check |
| POST | `/auth/register` | - | Register user |
| POST | `/auth/login` | - | Login with `username` + `password` form fields |
| GET | `/auth/me` | JWT | Current user |
| GET | `/categories` | - | List categories with `{ items, total, skip, limit }` |
| GET | `/categories/{id}` | - | Get category |
| POST | `/categories` | JWT | Create category |
| PATCH | `/categories/{id}` | JWT | Update category |
| DELETE | `/categories/{id}` | JWT | Delete category |
| GET | `/items` | - | List items with filters and pagination |
| GET | `/items/{id}` | - | Get item |
| GET | `/items/stats/summary` | - | Summary statistics |
| POST | `/items` | JWT | Create item |
| PATCH | `/items/{id}` | JWT | Update item |
| DELETE | `/items/{id}` | JWT | Delete item |

Implemented item filters:

- `min_price`
- `max_price`
- `category_id`
- `name_contains`

Rate limiting (FastAPI parity):

- Auth endpoints: 10 requests per minute per client
- Write endpoints: 60 requests per minute per client

## Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | ASP.NET Core environment |
| `ASPNETCORE_URLS` | `http://127.0.0.1:8010` | Bind address and port |
| `JWT_SECRET` | `change-me-in-production` | Reserved for upcoming auth work |
| `JWT_EXPIRE_MINUTES` | `60` | Reserved for upcoming auth work |
| `DATABASE_CONNECTION` | `Data Source=./data/app.db` | Reserved for upcoming SQLite work |

## Suggested next steps

1. Replace the in-memory store with EF Core + SQLite/PostgreSQL persistence.
2. Expand the test suite further toward full FastAPI count parity.
3. Add migrations and production persistence wiring.

## *-101 Family

### API backends

| Repo | Port | Type | Stack |
|------|------|------|-------|
| `fastAPI-101` | 8000 | API-only | FastAPI, SQLAlchemy |
| `nest-101` | 8006 | API-only | NestJS, TypeScript |
| `express-101` | 8007 | API-only | Express, Vitest |
| `fortran-101` | 8008 | API-only | Fortran, fpm |
| `java-101` | 8009 | API-only | Spring Boot, JPA, Flyway |
| `dotNet-101` | 8010 | API-only | ASP.NET Core, xUnit |

### Monolith references

| Repo | Port | Type |
|------|------|------|
| `django-101` | 8001 | Monolith + shop |
| `symfony-101` | 8002 | Monolith + shop |
| `laravel-101` | 8003 | Monolith + shop |

### Suggested pairing

- Compare API stacks: `java-101` on `8009` vs `dotNet-101` on `8010`
- Pair with a frontend: `react-101`, `vue-101`, or `flutter-101`
- Use `laravel-101` as the full contract reference for future auth and CRUD parity
