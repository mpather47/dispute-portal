# Dispute Portal

A full-stack bank dispute management system. Customers can raise disputes against transactions; admins review and update case statuses. Built with ASP.NET Core 8 and React 19.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| API | ASP.NET Core 8, Entity Framework Core 8, ASP.NET Identity |
| Database | SQL Server 2022 |
| Auth | JWT Bearer tokens, rate-limited login (5 req/min per IP) |
| Frontend | React 19, TypeScript, Vite, Axios, React Router |
| Serving | nginx (production), Vite dev server (development) |
| Containers | Docker, Docker Compose |
| CI | GitHub Actions |

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — for the full containerised stack
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — for running the API locally
- [Node.js 20+](https://nodejs.org/) — for running the frontend locally

---

## Quick Start — Full Stack (Docker Compose)

1. Copy the environment file:

   ```bash
   cp .env.example .env
   ```

   The `.env.example` already contains working development values. Edit them if you want to use different credentials.

2. Start everything:

   ```bash
   docker-compose up --build
   ```

   | Service | URL |
   |---------|-----|
   | React client | `http://localhost:3000` |
   | ASP.NET Core API | `http://localhost:7000` |
   | Swagger UI | `http://localhost:7000/swagger` |
   | Health check | `http://localhost:7000/health` |

   Migrations run automatically on API startup. The database is seeded with test accounts on first run (see [Test Accounts](#test-accounts)).

3. Log in with the credentials below and start exploring.

> To stop everything: `docker-compose down`  
> To also wipe the database volume: `docker-compose down -v`

---

## Local Development

### 1. Start the database

```bash
docker-compose up sqlserver
```

### 2. Run the API

Configure user secrets so the API can connect:

```bash
cd DisputePortal.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=DisputePortalDb;User Id=sa;Password=<your-sa-password>;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "<your-jwt-key>"
```

Then run:

```bash
dotnet run
```

The API starts on `http://localhost:7000`. Migrations and seed data run automatically.

### 3. Run the frontend

```bash
cd dispute-portal-client
cp .env.example .env        # sets VITE_API_BASE_URL=http://localhost:7000
npm install
npm run dev
```

The dev server starts at `http://localhost:5173`.

---

## Environment Variables

### Root `.env` (used by Docker Compose)

| Variable | Description |
|----------|-------------|
| `MSSQL_SA_PASSWORD` | SQL Server SA password (must meet complexity requirements) |
| `JWT_KEY` | JWT signing key — base64-encoded 256-bit secret |
| `ALLOWED_ORIGINS` | Frontend origin allowed by CORS (e.g. `http://localhost:3000`) |

Generate a secure JWT key:
```bash
openssl rand -base64 32
```

### `dispute-portal-client/.env` (used by Vite)

| Variable | Description |
|----------|-------------|
| `VITE_API_BASE_URL` | Base URL of the API (e.g. `http://localhost:7000`) |

---

## Testing

### .NET (xUnit + Moq)

```bash
dotnet test DisputePortal.Tests/DisputePortal.Tests.csproj
```

49 tests covering services, controllers, and middleware. Uses an in-memory EF Core database — no SQL Server required.

### React (Vitest + React Testing Library)

```bash
cd dispute-portal-client
npm test -- --run
```

30 tests covering page components, API integration, and the error boundary.

---

## CI Pipeline

GitHub Actions runs on every push and pull request to `master`:

1. **.NET Tests** — restores, builds, and runs all 49 xUnit tests
2. **React Tests** — type checks, lints (zero warnings), and runs all 30 Vitest tests
3. **Docker Build** — builds both images to confirm Dockerfiles are valid (runs only if both test jobs pass)

See [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

---

## Test Accounts

These accounts are created by the database seeder on first run:

| Role | Email | Password |
|------|-------|----------|
| Customer | `customer@test.com` | `Password123!` |
| Admin | `admin@test.com` | `Password123!` |

---

## API Reference

With the API running, open Swagger at `http://localhost:7000/swagger`.

**Typical workflow:**

1. `POST /api/auth/login` — authenticate and copy the returned `token`
2. Click **Authorize** in Swagger and enter `Bearer <token>`
3. Use the transaction and dispute endpoints as a customer, or the admin endpoints with an admin token

**Key endpoints:**

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/auth/login` | Authenticate |
| `GET` | `/api/transactions` | List customer transactions |
| `POST` | `/api/disputes` | Submit a dispute |
| `GET` | `/api/disputes/my` | List customer's disputes |
| `GET` | `/api/disputes/{id}` | Dispute details |
| `GET` | `/api/admin/disputes` | All disputes (paginated, admin only) |
| `PUT` | `/api/admin/disputes/{id}/status` | Update dispute status (admin only) |
| `GET` | `/api/notifications` | Notifications for current user |
| `GET` | `/health` | Health check |

---

## Project Structure

```
├── .github/
│   └── workflows/
│       └── ci.yml                  # GitHub Actions CI pipeline
├── DisputePortal.Api/              # ASP.NET Core API
│   ├── Controllers/                # HTTP endpoints
│   ├── Data/                       # EF Core DbContext and migrations
│   ├── DTOs/                       # Request/response models
│   ├── Middleware/                  # Global exception handler
│   ├── Models/                     # Domain entities
│   └── Services/                   # Business logic
├── DisputePortal.Tests/            # xUnit test project
│   ├── Controllers/                # Controller tests
│   └── Services/                   # Service tests
├── dispute-portal-client/          # React frontend
│   ├── src/
│   │   ├── api/                    # Axios client and API functions
│   │   ├── components/             # Shared components (Navbar, ErrorBoundary, toasts)
│   │   ├── context/                # React context (notifications)
│   │   ├── hooks/                  # Custom hooks (notification polling)
│   │   ├── pages/                  # Route-level page components
│   │   └── types/                  # TypeScript interfaces
│   ├── .env.example                # Frontend environment template
│   ├── Dockerfile                  # nginx production image
│   └── nginx.conf                  # nginx config with security headers and CSP
├── .env.example                    # Docker Compose environment template
└── docker-compose.yml              # Full stack: API + client + SQL Server
```
