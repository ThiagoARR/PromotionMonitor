# Feature status — Phase 1 MVP

See the durable copy in the agent store (`docs/feature-status.md`). Summary:

**Implemented:** JWT auth, monitoring CRUD, Hangfire collector, Mock + HTML/JSON-LD sources (demo mode), price history, effective-cost calculator, Console/Email alerts with idempotency, dashboard/detail UI, SSRF checks, Swagger/health/logs, Compose, automated tests.

**Pending:** advanced coupons/cashback, real CEP shipping, WhatsApp, Web Push/PWA, NLP, Redis/queues, extra retailers.

**Credential-dependent:** SMTP (simulated when empty); live HTML needs public structured markup; replace JWT key outside local demos.
