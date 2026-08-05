# Grok Career Coach

AI-assisted career coaching API and web app for job seekers.

**Status:** In active development (MIT portfolio project)  
**Author:** [Dingchen (Barry) Yuan](https://github.com/Dingchen-Yuan)  
**Live portfolio:** https://dingchen-yuan.github.io

---

## Overview

Users sign in with Google, submit a job description and résumé highlights, and
receive structured coaching output (fit analysis, interview questions,
improvement suggestions). Analysis uses **xAI Grok** when `GROK_API_KEY` is set;
otherwise a deterministic **mock coach** keeps local demos working. Responses are
cached briefly in **Redis**. PostgreSQL stores users and coaching sessions. Azure
Container Apps + Key Vault deployment scripts live under `deploy/azure/`.

## Current stack

| Layer | Technology | Status |
|--------|------------|--------|
| API | ASP.NET Core (C#) | Working |
| Auth | Google Identity Services + JWT (access + refresh) | Working |
| Data | PostgreSQL + EF Core | Working |
| Cache | Redis (analyze response cache, 15 min) | Working |
| AI | xAI Grok when keyed; otherwise MockGrokClient | Working |
| Containers | Docker Compose (web + API + Postgres + Redis) | Working |
| Docs | OpenAPI (Development) | Working |
| Tests | xUnit + mocked Grok client | Working |
| Cloud | Azure Container Apps + Key Vault | Scripts ready |
| Frontend | React + TypeScript | Working |

## Features (roadmap)

- [x] Repository & architecture scaffold
- [x] Google sign-in with JWT access and refresh tokens
- [x] Persist users & coaching sessions in PostgreSQL
- [x] JD × résumé analysis endpoint (`POST /api/coaching/analyze`)
- [x] Redis short-lived response cache for analyze
- [x] Docker Compose for local web + API + Postgres + Redis
- [x] OpenAPI + automated tests (Grok mocked)
- [x] Azure deploy scripts (Container Apps + Postgres + Redis + Key Vault)
- [x] Public production URL (New Zealand North Container Apps)

## Architecture (current)

```text
React UI ──JWT──▶ ASP.NET Core API ──▶ Grok (xAI) or Mock
                       │
                       ├── PostgreSQL
                       └── Redis (analyze cache)
```

## Local development

Without `GROK_API_KEY`, the API uses `MockGrokClient`, so no Grok key is required
for the end-to-end flow. Authentication uses Google Identity Services and
Google's official .NET token validator. The API issues short-lived access tokens,
rotates refresh tokens, and stores only refresh-token hashes in PostgreSQL.

Before starting, create a Google OAuth 2.0 Web client and add
`http://localhost:5173` as an authorized JavaScript origin. Copy its client ID to
`GOOGLE_CLIENT_ID` in `.env`.

```bash
# Start the React UI, API, PostgreSQL, and Redis
cp .env.example .env
docker compose up --build
```

EF Core migrations are applied automatically when the API starts.

Then open:

- Web UI: http://localhost:5173
- API health: http://localhost:5000/health
- OpenAPI document: http://localhost:5000/openapi/v1.json

Optional: set `GROK_API_KEY` in `.env` to call the real xAI Chat Completions API.

If ports are already occupied, set `API_PORT` and `POSTGRES_PORT` in `.env`.

To run the projects without Docker:

```bash
dotnet tool restore
dotnet ef database update --project src/GrokCareerCoach.Api

# Redis should be available at localhost:6379
ASPNETCORE_URLS=http://localhost:5000 \
  dotnet run --no-launch-profile --project src/GrokCareerCoach.Api

cd web
npm install
npm run dev
```

Run the checks:

```bash
dotnet test

cd web
npm run build
npm run lint
```

## Azure deployment

See **[deploy/azure/README.md](deploy/azure/README.md)**.

```bash
az login
./deploy/azure/deploy.sh
```

After deploy, add the printed web URL as a Google OAuth **Authorized JavaScript
origin**. Secrets (`GOOGLE_CLIENT_ID`, `JWT_SECRET`, `GROK_API_KEY`, DB/Redis
connection strings) are stored in Key Vault; the API reads them with a managed
identity when `KEY_VAULT_URI` is set.

## Project structure

```text
src/GrokCareerCoach.Api/          ASP.NET Core API, Grok client, Redis cache
tests/GrokCareerCoach.Api.Tests/  xUnit tests
web/                              React + TypeScript UI
deploy/azure/                     Azure Container Apps deploy script + docs
docker-compose.yml                Local application stack
.github/workflows/ci.yml          Build and test
```

## Environment variables

Copy `.env.example` to `.env` for local overrides. Never commit real API keys or
production JWT secrets.

## CV / résumé blurb

> Building an ASP.NET Core career-coaching API with Google JWT auth, PostgreSQL,
> Redis response caching, Docker Compose, optional xAI Grok analysis, and Azure
> Container Apps + Key Vault deployment.

## License

MIT
