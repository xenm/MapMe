# MapMe – TODO List (Canonical)

This is the authoritative, living TODO list for the MapMe project. It reflects the current state and plans, not historical fixes.

Last updated: 2025-08-30

## 🔴 High Priority

- [x] ~~CosmosDB – Test Behavior Standardization~~ **COMPLETED**
  - [x] ~~Ensure tests are isolated from Cosmos unless explicitly opted-in~~ ✅ Tests use InMemoryRepositories
  - [x] ~~Default integration tests to in-memory repositories even when Cosmos config is present~~ ✅ WebApplicationFactory overrides services
  - [ ] Add developer doc for running tests against the local Cosmos emulator (start/stop scripts, required config)
  - [x] ~~Prevent startup-time Cosmos calls in test host (WebApplicationFactory) or gracefully short-circuit~~ ✅ Services replaced in test setup

- [ ] Production – Fail Fast Without Cosmos
  - [ ] On Production environment, application must NOT start unless Cosmos connection is valid
  - [ ] Implement startup health check for configured database/containers
  - [ ] Add clear error messages and logs on startup failure
  - [ ] Document production configuration and required environment variables/secrets

- [x] ~~Documentation Reorganization (Current State, Not Fix Logs)~~ **COMPLETED**
  - [x] ~~Make docs/README.md the canonical index with clear navigation~~ ✅ docs/README.md provides structured index
  - [x] ~~Ensure every doc describes the current state; remove/merge "fix summaries"~~ ✅ Archive folder emptied, content integrated
  - [x] ~~Group docs by topic (Getting Started, Architecture, Configuration, Security, Testing, Features, Maps & JS Interop, Deployment & Ops, API)~~ ✅ Structured documentation hierarchy
  - [x] ~~Link all docs from index with concise descriptions~~ ✅ docs/README.md provides comprehensive linking
  - [x] ~~Keep only README.md and WIKI.md in the repository root; link everything else in docs/~~ ✅ Clean root structure

## 🟠 Medium Priority

- [ ] CI/CD
  - [ ] Add docs build validation (broken links, orphan pages)
  - [ ] Publish test reports and code coverage artifacts

- [ ] Packages & Security
  - [ ] Review/refresh NuGet packages to latest stable
  - [x] ~~Ensure Newtonsoft.Json is not referenced anywhere (prefer System.Text.Json)~~ ✅ SystemTextJsonCosmosSerializer implemented with comprehensive tests

- [ ] Testing Enhancements
  - [ ] Add performance/load test plan (locust/k6/GH actions runner)
  - [ ] Introduce API contract tests (OpenAPI-based) once Swagger is added

## 🟡 Low Priority

- [ ] Developer Experience
  - [ ] Provide VS Code tasks/launch for running tests and app
  - [ ] Add sample .env templates for local dev

---

# Legacy Backlog (to re-triage under the new structure)

These items were migrated from the previous root TODO and should be re-categorized progressively. Strike-through items indicate already implemented features.

## Infrastructure & Configuration
- [ ] Update run configurations (VS Code tasks, launch, IDE-agnostic)
- [ ] CI/CD pipeline setup (build/test on PR, reports, deployment pipeline)

## Testing & Quality
- [ ] Performance benchmarking tests
- [ ] Load testing scenarios for API endpoints
- [ ] Security testing (input validation, XSS, etc.)
- [ ] Code coverage reporting
- [ ] UI component tests for Blazor components
- [ ] End-to-end browser automation tests
- [ ] Database integration tests (real Cosmos DB)
- [ ] API contract testing
- [ ] Optimize test execution time
- [ ] Test data builders/factories
- [ ] Test categorization for different environments
- [ ] Mutation testing

## Architecture & Code Quality
- [ ] Security enhancements (rate limiting, CORS for production, API key management)
- [ ] Performance optimizations (caching, compression, lazy loading, profiling)
- [ ] Static analysis (SonarCloud/CodeQL), comprehensive error handling, monitoring
- [ ] Coding standards, XML docs for public APIs

## Features & Functionality
- [ ] User onboarding flow, preferences and settings, photo uploads
- [ ] Matching algorithms, real-time notifications
- [ ] Advanced search & filtering, recommendation engine
- [ ] Social media integration, geolocation-based features

## Documentation
- [ ] OpenAPI/Swagger documentation and usage examples
- [ ] Authentication flow docs, rate limiting docs
- [ ] ADRs, deployment procedures, troubleshooting, contribution guidelines, review checklist

## Deployment & Operations
- [ ] Production environment configuration
- [ ] Health checks & monitoring, logging & alerting
- [ ] Backups & DR, deployment scripts
- [ ] APM, analytics, error tracking, dashboards, automated alerting

## Maintenance
- [ ] Monthly dependency updates, security patch reviews, performance monitoring
- [ ] Test suite maintenance, documentation upkeep
- [ ] Quarterly technical debt & architecture review, security and dependency audits

