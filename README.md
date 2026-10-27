# PromotionMonitor

Phase 1 MVP: register product watches (URL, voltage, max price), collect price history from mock / structured HTML sources, compute an auditable **pay-at-checkout** vs **net-after-cashback** breakdown, and deliver alerts on Console/Email channels with idempotency.

## Stack

- **API:** .NET 8, ASP.NET Core, EF Core, SQL Server, Hangfire, Serilog, Swagger, JWT
- **Web:** React, TypeScript, Vite, Tailwind CSS, shadcn-style primitives, Recharts
- **Local runtime:** Docker Compose (SQL Server + API + web)

## Quick start (Docker Compose)

```bash
cp .env.example .env
docker compose up --build
```

- Web UI: http://localhost:45173
- API / Swagger: http://localhost:45881/swagger
- Health: http://localhost:45881/health
- Hangfire: http://localhost:45881/hangfire

Default SQL SA password is in `.env.example` (local only). SMTP left empty → email delivery is **clearly simulated**.

> Compose wires the API to SQL through `host.docker.internal` (published host port). That avoids environments where container-to-container traffic on `:1433` is filtered; on a normal Docker Desktop/Linux bridge you can switch the connection string host back to `sqlserver` if you prefer.

### Demo flow

1. Open the web UI → **Create an account** → sign in.
2. **New monitoring** with URL `https://example.com/products/demo-airfryer-110`, voltage `110V`, max price `450`, source **Mock catalog**, demo mode on.
3. Open the detail page → **Check now** → inspect pay-at-checkout breakdown and chart.
4. Alerts appear under **Alerts** (Console provider always; Email simulated without SMTP).

Mock catalog keys (embed in URL or model): `demo-airfryer-110`, `demo-airfryer-220`, `demo-airfryer-bivolt`, `demo-notebook`, `demo-phone`. URLs containing `fail` simulate source failure (**no invented prices**).

## Local development (without full compose)

### Prerequisites

- .NET 8 SDK
- Node 22+
- SQL Server (Compose `sqlserver` service is enough)

```bash
docker compose up sqlserver -d
dotnet ef database update --project src/PromotionMonitor.Infrastructure --startup-project src/PromotionMonitor.Api
ASPNETCORE_URLS=http://127.0.0.1:45881 dotnet run --project src/PromotionMonitor.Api
cd web && npm install && npm run dev
```

Web Vite port: **45173** (proxies `/api` to the API).

### Tests

```bash
dotnet test
```

## Configuration

| Variable | Purpose |
|----------|---------|
| `ConnectionStrings__Default` | SQL Server |
| `Jwt__Key` | JWT signing key (required, from env in real deploys) |
| `Smtp__*` | Real email; empty = simulated provider |

See `.env.example`. Secrets must not be committed.

## Architecture & status

- Plan: agent store `docs/phase-1-plan.md`
- Architecture: agent store `docs/architecture.md`
- Feature status (implemented / pending / credential-dependent): agent store `docs/feature-status.md`

## Security notes (Phase 1)

- User product URLs are validated against SSRF (no loopback/private IPs / metadata hosts).
- Adapters never bypass CAPTCHA/antibot; failures store errors without inventing prices.
- Money uses `decimal`. Observations keep raw + normalized payloads for audit.
- Cashback is estimated and **never** reduces `PayAtCheckout`.

## Phase 2+ (not in this MVP)

Advanced coupons/cashback, real shipping by CEP, WhatsApp Cloud API, PWA/Web Push, NLP matching, distributed queues, additional retailer adapters.
