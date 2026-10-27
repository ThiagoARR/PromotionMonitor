# PromotionMonitor — Architecture (Phase 1)

## Goals

Ship a usable vertical slice: register a product without code changes, filter voltage + max price, keep history, compute auditable effective cost, notify on a configured channel — while keeping the design open for new monitoring types later.

## Modular backend

```
src/
  PromotionMonitor.Domain/          Entities & enums (no infra deps)
  PromotionMonitor.Application/     Ports, calculators, collectors, DTOs
  PromotionMonitor.Infrastructure/  EF Core, Hangfire, sources, notifications, JWT
  PromotionMonitor.Api/             HTTP endpoints, Swagger, health, DI host
tests/
  PromotionMonitor.Tests/
web/                                React SPA
```

### Domain (core types)

| Entity | Role |
|--------|------|
| `UserAccount` | Auth identity + notification preferences |
| `Monitoring` | User-owned watch (URL, voltage, max/target, schedule, source kind, demo flag) |
| `PriceObservation` | Raw + normalized capture; success/failure; fingerprint for dedupe |
| `PriceCalculation` | Auditable breakdown linked 1:1 to a successful observation |
| `NotificationDelivery` | Channel delivery with unique `IdempotencyKey` |

`SourceKind` and `NotificationChannel` are enums so new adapters/providers register without rewriting core entities. Future monitoring *types* can add fields/strategies beside `Monitoring` without collapsing into one scraper.

### Application ports

- `IPriceSource` — fetch offer (`Mock` / `HtmlStructured` today)
- `INotificationProvider` — Console + Email (WhatsApp/WebPush stubs reserved)
- `IEffectiveCostCalculator` — central money math
- `IUrlSafetyValidator` — SSRF guard for user URLs
- `IMonitoringCollector` / `IAlertDispatcher` — orchestration

### Effective cost rules

```
PayAtCheckout     = Listed + Shipping − ConfirmedCoupon
CashbackEstimate  = informational only
NetAfterCashback  = PayAtCheckout − CashbackEstimate
```

Cashback **never** reduces checkout. Unconfirmed coupons are ignored. Values are `decimal` with 2-place rounding.

### Collection pipeline

1. Hangfire recurring job (`minutely`) calls `CollectDueAsync`.
2. Due monitorings invoke the registered `IPriceSource`.
3. Failures → observation with `Success=false`, **no price invented**.
4. Success → calculation persisted; if voltage matches (or unknown≠false) and `PayAtCheckout ≤ MaxPrice`, alerts dispatch.
5. Fingerprint + delivery idempotency key suppress duplicate spam.

### Phase 2 enrichment

| Piece | Role |
|-------|------|
| `CouponRule` + `ICouponEvaluator` | Best applicable coupon; only **confirmed** reduces checkout |
| `CashbackRule` + `ICashbackEvaluator` | Best program estimate; never reduces `PayAtCheckout` |
| `IShippingQuoteProvider` | CEP quotes — `SimulatedCep` or live HTTP when configured |
| `MonitoringStoreLink` / `StoreOffer` | Multi-store fetch per round; rank by checkout / net-after-cashback |

Collector flow: for each store link → fetch price → quote shipping by CEP → evaluate coupon/cashback → persist `StoreOffer` + primary `PriceObservation`.

### Frontend

SPA talks to `/api/*` (Vite proxy locally; nginx proxy in Compose). Screens: auth, dashboard, create watch, detail (breakdown, comparison table, Recharts), **Rules** (coupons/cashback/shipping probe).

### Runtime topology

```
Browser → web:45173 (nginx) → api:45881 → SQL Server
                              ↘ Hangfire workers (in-process)
```

Compose currently routes API→SQL and web→API through `host.docker.internal` (published host ports) so the stack still runs on hosts where Docker bridge filtering blocks container-to-container traffic to service ports. On a normal Desktop/Linux bridge you can point those hosts back at the Compose service names (`sqlserver`, `api`).

### Extension points (later phases)

- New `IPriceSource` implementations per retailer/feed
- WhatsApp / Web Push `INotificationProvider`s
- NLP / smart matching
- Additional monitoring kinds (availability, bundle deals) as strategy or subtype
