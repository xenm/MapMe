# MapMe — A Vibe‑Coded, AI‑Accelerated, Map‑Based Dating App

![.NET](https://img.shields.io/badge/.NET-10-blue) ![Blazor](https://img.shields.io/badge/Blazor-WASM%20%2B%20Interactive%20SSR-purple) ![Tests](https://img.shields.io/badge/Tests-Unit%20%2F%20Integration-green) ![License](https://img.shields.io/badge/License-Open%20Source-lightgrey)

MapMe is an experimental, open‑source dating app built to explore modern AI tooling, multi‑tech stacks, and production‑grade workflows—from local dev to Azure hosting with CI/CD, monitoring, and comprehensive testing. It blends Blazor WebAssembly + Interactive SSR + Google Maps JS interop, with a clear path to .NET MAUI and/or Flutter.

This project is both a learning lab and a real app: rethinking dating platforms—transparent, community‑driven, and engineered for quality, reliability, and extensibility.

---

## Table of Contents

- Vision
- Tech Stack
- Key Features
- Repository Layout
- Architecture Overview
- Getting Started
- Testing & Quality Gates
- Development Workflow (Vibe Coding + AI)
- CI/CD (Azure) Roadmap
- Security & Privacy
- Contributing
- Roadmap
- Documentation Index
- Why This Matters
- Badges

---

## Vision

- __Purpose__: Explore AI‑assisted development (“Vibe Coding”) across varied stacks while building a real product, ending with Azure hosting, automated pipelines, and deep quality gates.
- __Thesis__: Testing AI models on underused tech and pre‑release frameworks pushes ecosystem capabilities.
- __Outcome__: A clean, reliable, extendable, and well‑documented app with automation and quality checks—plus a community effort to improve how dating apps work.

---

## Tech Stack

- __Current__: .NET 10, ASP.NET Core Minimal APIs, Blazor WebAssembly + Interactive SSR, System.Text.Json, JS interop with Google Maps
- __Testing__: xUnit, Unit + Integration + Service‑Level tests, PowerShell test runners
- __Data__: In‑memory repositories (dev/demo), Azurite (local), Cosmos DB‑ready integration
- __Frontend__: Bootstrap‑based responsive UI
- __Future__: .NET MAUI and/or Flutter, SignalR real‑time messaging

---

## Key Features

- __Map‑first experience__: Create and view Date Marks (ratings, tags, qualities, visit dates, visibility, Google Maps links).
- __Rich profiles__: Tinder‑style fields (bio, age, preferences, lifestyle, photos with captions/order).
- __Chat__: Conversations, unread counts, read status, archiving, deletion.
- __Unified pages__: Profile (`/profile`) and User (`/user/{username}`) share the same layout; edit controls only on Profile.
- __Google Maps integration__: Create marks from places, clickable Google links in popups/lists, “View on Map” deep‑linking.
- __Duplicate prevention__: Date Marks de‑duped by PlaceId + UserId.
- __Local persistence__: Client caching and storage for smooth dev/demo flows.

---

## Repository Layout

- `MapMe/MapMe/MapMe/` — Server app (Minimal APIs, DI, in‑memory repos). Key: `Program.cs`
- `MapMe/MapMe.Client/` — Blazor WASM + Interactive SSR client (pages, components, JS interop)
  - Pages: `Pages/Map.razor`, `Pages/Profile.razor`, `Pages/User.razor`
  - Interop: `wwwroot/js/mapInitializer.js`
- `MapMe/MapMe.Tests/` — Unit + Integration tests; PS scripts and docs
- `docs/` — Architecture, features, Cosmos DB integration, testing, troubleshooting
- `Scripts/` — Azurite/Cosmos helpers: `start-cosmos.ps1`, `init-cosmosdb.ps1`, `stop-cosmos.ps1`
- `docker-compose.yml`, `docker-compose.cosmos.yml` — Local orchestration

---

## Architecture Overview

- __Pattern__: Minimal APIs + DI + repository pattern
- __Contracts__: Shared DTOs aligned on both client and server (System.Text.Json)
- __Persistence__: In‑memory repos by default; Cosmos DB path documented and script‑assisted
- __Interop__: Blazor <-> JS via `mapInitializer.js` for map events and popups
- __Auth Placeholder__: Header‑based `X-User-Id` for tests; ready to swap for real auth

---

## Getting Started

__Prerequisites__
- .NET 10 SDK
- PowerShell Core (macOS/Linux compatible)
- Optional: Docker (Azurite/Cosmos emulator)

__Build__
- `dotnet build`

__Run (Dev)__
- Server: `dotnet run --project MapMe/MapMe/MapMe`
- Client: `dotnet run --project MapMe/MapMe.Client`

__Local Cosmos (optional)__
- Start: `pwsh ./Scripts/start-cosmos.ps1`
- Initialize: `pwsh ./Scripts/init-cosmosdb.ps1`
- Stop: `pwsh ./Scripts/stop-cosmos.ps1`
- Or: `docker compose -f docker-compose.cosmos.yml up -d`

---

## Testing & Quality Gates

Project rule: __build and run Service‑Level tests after each change__.

- Unit: `dotnet test MapMe/MapMe.Tests --filter TestCategory=Unit`
- Integration: `dotnet test MapMe/MapMe.Tests --filter TestCategory=Integration`
- All tests (PowerShell, cross‑platform):
  - `test-unit.ps1`
  - `test-integration.ps1`
  - `test-all.ps1`
- Features:
  - TRX → HTML conversion (if available)
  - `-OutputDir` and `-NoHtml` controls
- Coverage spans chat, Date Marks, profiles, error handling, and E2E API flows with high pass rates.

---

## Development Workflow (Vibe Coding + AI)

- __Standards__: .NET 10, System.Text.Json, async/await best practices, DI + repositories, XML docs, and up‑to‑date `docs/`
- __Branching__: `main` (stable), `feature/*` (scoped); PRs require passing tests + updated docs
- __Automations (planned)__: CodeQL, Roslyn analyzers, formatting, spellcheck, SL test gates, conventional commits, changelogs

---

## CI/CD (Azure) Roadmap

- __Pipelines__: Build + test matrix; artifact packaging (WASM + server); CodeQL and analyzers; PR gates
- __Environments__: Dev (preview), Staging (smoke/load), Prod (blue/green or canary)
- __Infra__: Azure App Service/Static Web Apps (WASM), optional Functions, Cosmos DB
- __Monitoring__: App Insights + Azure Monitor; distributed tracing, dashboards, alerts
- __Ops__: Health checks with auto‑rollback, backups, data lifecycle policies

---

## Security & Privacy

- Input validation, model binding hardening
- Rate limiting and anti‑abuse controls
- Auth abstraction ready for Entra ID/OAuth providers
- Data minimization and PII care; Key Vault for secrets
- Threat modeling and secure defaults

---

## Contributing

- Look for `good-first-issue` and `help-wanted`
- Add tests with every feature/bug fix
- Update `docs/` for user‑visible changes
- Keep Blazor JS interop consistent (`OnXxxAsync` naming)
- Avoid Newtonsoft.Json; use System.Text.Json

---

## Roadmap

- __Short‑Term__: SignalR real‑time chat; Cosmos DB repositories; Azure CI/CD; stronger auth/roles; perf & a11y tests
- __Mid‑Term__: .NET MAUI client; Flutter client; push notifications; feature flags/A‑B testing
- __Long‑Term__: Community‑driven recommendation engine; transparent algorithms; federated learning

---

## Documentation Index

- `docs/README.md` — Docs landing
- `docs/features/Chat.md`, `docs/features/DateMarks.md`, `docs/features/DateMarkEdit.md` — Feature guides
- `docs/CosmosDB-Integration.md` — Cosmos/Azurite setup
- `MapMe.Tests/README.md` — Test suite usage and structure

---

## Why This Matters

MapMe challenges the status quo by empowering a community to build ethical, transparent, and delightful dating experiences. It demonstrates how AI‑assisted engineering + rigorous software discipline produce production‑ready systems—and invites you to collaborate.

__Join in—fork, star, and open an issue/PR.__
