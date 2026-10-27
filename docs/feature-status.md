# Feature status — PromotionMonitor

See the durable copy in the agent store (`docs/feature-status.md`).

**Phase 1:** JWT, monitoring CRUD, Hangfire, mock/HTML sources, history, effective cost, alerts, dashboard, SSRF, Compose, tests.

**Phase 2:** Coupon rules, cashback rules, CEP shipping (simulated or HTTP), multi-store comparison + UI (`/rules`, comparison table).

**Pending:** WhatsApp/Web Push/PWA, NLP, Redis, more live retailers.

**Credential-dependent:** SMTP simulated when empty; Shipping HTTP falls back to `SimulatedCep`; live HTML needs public structured markup.
