# Grok Career Coach

AI-assisted career coaching API and web app for job seekers.

**Status:** In active development (MIT portfolio project)  
**Author:** [Dingchen (Barry) Yuan](https://github.com/Dingchen-Yuan)  
**Live portfolio:** https://dingchen-yuan.github.io

---

## Overview

Users sign in, submit a job description and résumé highlights, and receive structured coaching output (fit analysis, interview questions, improvement suggestions) powered by **xAI Grok**. The API is secured with **JWT** and deployed on **Azure**.

## Planned / target stack

| Layer | Technology |
|--------|------------|
| API | ASP.NET Core (C#) |
| Auth | Google Identity Services + JWT (access + refresh) |
| Data | PostgreSQL + EF Core |
| Cache / rate limit | Redis |
| AI | xAI Grok (server-side only) |
| Containers | Docker Compose |
| Docs | OpenAPI / Swagger |
| Tests | xUnit + mocked Grok client |
| Cloud | Azure App Service / Container Apps + Key Vault |
| Frontend | React + TypeScript (minimal UI) |

## Features (roadmap)

- [x] Repository & architecture scaffold
- [x] Google sign-in with JWT access and refresh tokens
- [x] Persist users & coaching sessions in PostgreSQL
- [ ] Grok-backed “JD × résumé” analysis endpoint
- [ ] Redis rate limiting / short-lived response cache
- [x] Docker Compose for local web + API + Postgres + Redis
- [x] OpenAPI + automated tests (Grok mocked)
- [ ] Azure deploy with secrets in Key Vault

## Architecture (target)

```text
React UI ──JWT──▶ ASP.NET Core API ──▶ Grok (xAI)
                       │
                       ├── PostgreSQL
                       ├── Redis
                       └── Azure Key Vault (secrets)
```

## Local development

The scaffold currently uses a mock Grok client, so no Grok API key is needed to
try the end-to-end flow. Authentication uses the official Google Identity
Services client and Google's official .NET token validator. The app then issues
short-lived access tokens, rotates refresh tokens, and stores only refresh-token
hashes in PostgreSQL.

Before starting, create a Google OAuth 2.0 Web client and add
`http://localhost:5173` as an authorized JavaScript origin. Copy its client ID to
`GOOGLE_CLIENT_ID` in `.env`.

```bash
# Start the React UI, API, PostgreSQL, and Redis
cp .env.example .env
docker compose up --build
```

EF Core migrations are applied automatically when the API starts in the
Development environment.

Then open:

- Web UI: http://localhost:5173
- API health: http://localhost:5000/health
- OpenAPI document: http://localhost:5000/openapi/v1.json

If ports are already occupied, set `API_PORT` and `POSTGRES_PORT` in `.env`.

To run the projects without Docker:

```bash
dotnet tool restore
dotnet ef database update --project src/GrokCareerCoach.Api

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

## Project structure

```text
src/GrokCareerCoach.Api/       ASP.NET Core API and Grok client abstraction
tests/GrokCareerCoach.Api.Tests/  xUnit tests
web/                           React + TypeScript UI
docker-compose.yml             Local application stack
```

## Environment variables

Copy `.env.example` to `.env` for local overrides. Never commit real API keys or
production JWT secrets.

## CV / résumé blurb

> Building an ASP.NET Core career-coaching API with JWT, PostgreSQL, Redis, Docker, and xAI Grok, targeting deployment on Azure (App Service + Key Vault).

## License

MIT
