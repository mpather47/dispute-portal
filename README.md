# Dispute Portal

A full-stack bank dispute management system. Customers raise disputes against transactions, upload supporting attachments, and reply when more information is requested. Admins review cases, update statuses, and monitor live stats on a dashboard. Built with ASP.NET Core 8 and React 19.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| API | ASP.NET Core 8, Entity Framework Core 8, ASP.NET Identity |
| Database | SQL Server 2022 |
| Auth | JWT Bearer tokens |
| Real-time | SignalR (notifications, dispute-list refresh) |
| Frontend | React 19, TypeScript, Vite, Axios, React Router v7 |
| Serving | nginx (production), Vite dev server (development) |
| Containers | Docker, Docker Compose |

---

## Features

### Customer
- View transactions and flag any disputable charge
- Submit a dispute with a reason and supporting notes
- Upload file attachments (images, PDFs, text — max 5 MB) when more information is requested
- Reply to a dispute to move it back into review
- Track dispute status and full event timeline in real time
- Receive toast notifications and badge counts when status changes

### Admin
- Dashboard with live stats: total, open, submitted today, average resolution time, status breakdown
- Paginated and searchable dispute list (case number or customer name)
- Review dispute details, attachments, and customer reply history
- Transition disputes through a validated status machine (Submitted → Under Review → More Info Required → Approved / Rejected / Resolved)
- Notified in real time when a customer submits or replies to a dispute

### Security
- JWT auth with role-based access (Customer / Admin)
- Rate limiting: login 5 req/min per IP, file upload 10 req/min per IP
- Input sanitization on all free-text fields — HTML decoded then stripped before persistence (XSS prevention)
- `BusinessRuleException` hierarchy: user-facing rule violations return their message; unhandled `InvalidOperationException`s return a generic "Bad request." with no internal detail leakage
- File type and size validation on upload

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

   The `.env.example` contains working development defaults. Edit credentials if needed.

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

   Migrations run automatically on API startup (with retry logic to handle SQL Server cold-start). The database is seeded with test accounts on first run.

3. Log in with the credentials below and start exploring.

> To stop: `docker-compose down`  
> To also wipe the database: `docker-compose down -v`

---

## Local Development

### 1. Start the database

```bash
docker-compose up sqlserver
```

### 2. Run the API

```bash
cd DisputePortal.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=DisputePortalDb;User Id=sa;Password=<your-sa-password>;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "<your-jwt-key>"

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

### Root `.env` (Docker Compose)

| Variable | Description |
|----------|-------------|
| `MSSQL_SA_PASSWORD` | SQL Server SA password |
| `JWT_KEY` | JWT signing key — minimum 32 characters |
| `ALLOWED_ORIGINS` | CORS-allowed frontend origin (e.g. `http://localhost:3000`) |

Generate a secure key:
```bash
openssl rand -base64 32
```

### `dispute-portal-client/.env` (Vite)

| Variable | Description |
|----------|-------------|
| `VITE_API_BASE_URL` | Base URL of the API (e.g. `http://localhost:7000`) |

---

## Testing

### .NET — 75 tests (xUnit + Moq)

```bash
dotnet test DisputePortal.Tests/DisputePortal.Tests.csproj
```

Covers: services (dispute lifecycle, auth, stats, attachments, reply), controllers, middleware (exception mapping, `BusinessRuleException` exposure), and `InputSanitizer`. Uses EF Core InMemory — no SQL Server required.

### React — 88 tests (Vitest + React Testing Library)

```bash
cd dispute-portal-client
npm test -- --run
```

Covers: login flow, dispute pages, admin disputes and dashboard, notifications, status badge, error boundary.

---

## Test Accounts

Seeded automatically on first run:

| Role | Email | Password |
|------|-------|----------|
| Customer | `customer@test.com` | `Password123!` |
| Admin | `admin@test.com` | `Password123!` |

---

## API Reference

Full interactive docs at `http://localhost:7000/swagger`. Authenticate with `POST /api/auth/login`, copy the token, then click **Authorize** in Swagger.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/auth/login` | — | Authenticate, returns JWT |
| `GET` | `/api/transactions` | Customer | List own transactions |
| `POST` | `/api/disputes` | Customer | Submit a dispute |
| `GET` | `/api/disputes/my` | Customer | List own disputes |
| `GET` | `/api/disputes/{id}` | Customer / Admin | Dispute details + timeline + attachments |
| `POST` | `/api/disputes/{id}/reply` | Customer | Reply to a MoreInfoRequired dispute |
| `POST` | `/api/disputes/{id}/attachments` | Customer / Admin | Upload a file (MoreInfoRequired status only for customers) |
| `GET` | `/api/disputes/{id}/attachments/{aid}` | Customer / Admin | Download an attachment |
| `GET` | `/api/admin/disputes` | Admin | All disputes — paginated, filterable, searchable |
| `GET` | `/api/admin/disputes/stats` | Admin | Aggregate stats for dashboard |
| `PUT` | `/api/admin/disputes/{id}/status` | Admin | Transition dispute status |
| `GET` | `/api/notifications` | Any | List notifications for current user |
| `POST` | `/api/notifications/mark-all-read` | Any | Mark all notifications as read |
| `GET` | `/health` | — | Health check |

### Dispute status machine

```
Submitted ──► UnderReview ──► Approved
    │               │         Rejected
    │               └──► MoreInfoRequired ──► UnderReview (via customer reply)
    │                         │
    └──► MoreInfoRequired      └──► Rejected
    └──► Rejected
```

---

## Project Structure

```
├── DisputePortal.Api/
│   ├── Controllers/            # HTTP endpoints (Auth, Disputes, Admin, Notifications)
│   ├── Data/                   # AppDbContext, EF Core migrations, seed data
│   ├── DTOs/                   # Request / response records
│   ├── Exceptions/             # BusinessRuleException (user-facing 400s)
│   ├── Helpers/                # InputSanitizer (HTML decode + strip)
│   ├── Hubs/                   # SignalR NotificationHub
│   ├── Middleware/             # Global exception handler
│   ├── Models/                 # Domain entities
│   └── Services/               # Business logic (DisputeService, AuthService, NotificationService)
├── DisputePortal.Tests/
│   ├── Controllers/            # Controller unit tests
│   ├── Helpers/                # InputSanitizer tests
│   ├── Middleware/             # Exception handler tests
│   └── Services/               # Service unit tests
├── dispute-portal-client/
│   ├── src/
│   │   ├── api/                # Axios client and typed API functions
│   │   ├── components/         # Navbar, StatusBadge, ErrorBoundary, ToastContainer
│   │   ├── context/            # NotificationContext
│   │   ├── pages/              # LoginPage, TransactionsPage, DisputeDetailsPage,
│   │   │                       # AdminDashboardPage, AdminDisputesPage, NotificationsPage, …
│   │   └── types/              # TypeScript interfaces
│   ├── Dockerfile              # nginx production image
│   └── nginx.conf              # Reverse proxy + security headers
├── .env.example                # Docker Compose environment template
└── docker-compose.yml          # API + client + SQL Server
```
