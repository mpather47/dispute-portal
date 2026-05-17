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

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — for the containerised stack
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — for running the API locally
- [Node.js 20+](https://nodejs.org/) — for running the frontend locally

---

## Quick Start — Docker Compose (API + Database)

1. Copy the environment file and fill in your values:

   ```bash
   cp .env.example .env
   ```

2. Start the API and SQL Server:

   ```bash
   docker-compose up --build
   ```

   The API will be available at `http://localhost:7000`.  
   Migrations run automatically on startup. The database is seeded with test accounts (see [Test Accounts](#test-accounts) below).

3. Open the Swagger UI to explore and test the API:

   ```
   http://localhost:7000/swagger
   ```

> The `docker-compose.yml` runs the API and database only. To run the frontend in Docker, see [Building the Client Image](#building-the-client-image) below.

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
| `ALLOWED_ORIGIN` | Frontend origin allowed by CORS (e.g. `http://localhost:3000`) |

Generate a secure JWT key:
```bash
openssl rand -base64 32
```

### `dispute-portal-client/.env` (used by Vite)

| Variable | Description |
|----------|-------------|
| `VITE_API_BASE_URL` | Base URL of the API (e.g. `http://localhost:7000`) |

---

## Building the Client Image

```bash
cd dispute-portal-client
docker build -t dispute-portal-client .
docker run -p 3000:80 dispute-portal-client
```

The client will be available at `http://localhost:3000`.

> For production, update the `connect-src` directive in `nginx.conf` to match your API domain.

---

## Testing the API

With the API running, open Swagger at `http://localhost:7000/swagger`.

**Workflow:**

1. `POST /api/auth/login` — authenticate and copy the returned `token`
2. Click **Authorize** in Swagger and enter `Bearer <token>`
3. Use the transaction and dispute endpoints as a customer, or the admin endpoints with an admin token

### Test Accounts

These accounts are created by the database seeder on first run:

| Role | Email | Password |
|------|-------|----------|
| Customer | `customer@test.com` | `Password123!` |
| Admin | `admin@test.com` | `Password123!` |

---

## Project Structure

```
├── DisputePortal.Api/          # ASP.NET Core API
│   ├── Controllers/            # HTTP endpoints
│   ├── Data/                   # EF Core DbContext and migrations
│   ├── DTOs/                   # Request/response models
│   ├── Middleware/             # Global exception handler
│   ├── Models/                 # Domain entities
│   └── Services/               # Business logic
├── dispute-portal-client/      # React frontend
│   ├── src/
│   │   ├── api/                # Axios client and API functions
│   │   ├── components/         # Shared components (Navbar, ErrorBoundary, etc.)
│   │   ├── pages/              # Route-level page components
│   │   └── types/              # TypeScript interfaces
│   ├── .env.example            # Frontend environment template
│   ├── Dockerfile              # nginx production image
│   └── nginx.conf              # nginx config with security headers
├── .env.example                # Docker Compose environment template
└── docker-compose.yml          # API + SQL Server services
```
