# Grok Career Coach

AI-assisted career coaching app for job seekers — **live demo** on Azure.

**Author:** [Dingchen (Barry) Yuan](https://github.com/Dingchen-Yuan)  
**Portfolio:** https://dingchen-yuan.github.io  
**Live web:** https://ca-grok-web.thankfuldune-4d704de3.newzealandnorth.azurecontainerapps.io  
**Live API health:** https://ca-grok-api.thankfuldune-4d704de3.newzealandnorth.azurecontainerapps.io/health

---

## What it does

1. Sign in with **Google**
2. Paste a **job description** and **résumé highlights** (drafts stay in the tab if you refresh)
3. Get a structured coaching brief (fit summary, strengths, gaps, interview
   questions, improvements)
4. **Download the brief as a PDF** or **copy it to the clipboard**
5. Revisit past sessions from **saved history**, search them, and delete ones you no longer need

Analysis uses **xAI Grok** when `GROK_API_KEY` is set; otherwise a deterministic
**mock coach** keeps local demos working. Analyze responses are cached briefly in
**Redis**. Users and sessions live in **PostgreSQL**.

## Stack

| Layer | Technology |
|--------|------------|
| Frontend | React + TypeScript (Vite), PDF export via jsPDF |
| API | ASP.NET Core (C#) |
| Auth | Google Identity Services + JWT (access + refresh) |
| Data | PostgreSQL + EF Core |
| Cache | Redis (analyze responses, ~15 min) |
| AI | xAI Grok (optional) / MockGrokClient |
| Local | Docker Compose (web + API + Postgres + Redis) |
| Cloud | Azure Container Apps, ACR, Postgres, Redis, Key Vault |
| CI | GitHub Actions (`ci.yml`) |

## Architecture

```text
┌────────────────────┐         JWT          ┌──────────────────────────┐
│  React web (Vite)  │ ───────────────────▶ │  ASP.NET Core API        │
│  Google GIS button │                      │  Auth + Coaching APIs    │
│  PDF download      │ ◀─────────────────── │                          │
│  Copy brief        │                      │                          │
└────────────────────┘     coaching JSON    └────────────┬─────────────┘
                                                         │
                     ┌───────────────────────────────────┼────────────────┐
                     │                                   │                │
                     ▼                                   ▼                ▼
            ┌────────────────┐                 ┌────────────────┐  ┌─────────────┐
            │  PostgreSQL    │                 │  Redis cache   │  │  xAI Grok   │
            │  users /       │                 │  analyze       │  │  (optional) │
            │  sessions /    │                 │  responses     │  └─────────────┘
            │  refresh hashes│                 └────────────────┘
            └────────────────┘

Production (Azure Container Apps · New Zealand North)
  Web + API images → ACR → Container Apps
  Secrets → Key Vault (API managed identity)
```

## Features

- [x] Google sign-in with JWT access and refresh tokens
- [x] Persist users and coaching sessions in PostgreSQL
- [x] JD × résumé analysis (`POST /api/coaching/analyze`)
- [x] Redis short-lived response cache
- [x] Download coaching brief as PDF (browser-side)
- [x] Copy coaching brief to the clipboard
- [x] Delete saved coaching sessions
- [x] Search saved coaching history
- [x] Persist JD/résumé drafts in the browser tab
- [x] Docker Compose for local full stack
- [x] OpenAPI (Development) + xUnit tests (Grok mocked)
- [x] Azure deploy scripts (Container Apps + Postgres + Redis + Key Vault)
- [x] Public live demo (New Zealand North)

## Local development

```bash
cp .env.example .env
# Set GOOGLE_CLIENT_ID (and optionally GROK_API_KEY, JWT_SECRET)
docker compose up --build
```

Then open:

- Web UI: http://localhost:5173
- API health: http://localhost:5000/health (or `API_PORT` from `.env`)
- OpenAPI: http://localhost:5000/openapi/v1.json

Before first run, create a Google OAuth **Web** client and add
`http://localhost:5173` as an authorized JavaScript origin.

Without `GROK_API_KEY`, the API uses `MockGrokClient` so the full flow still works.

Checks:

```bash
dotnet test
cd web && npm run build && npm run lint
```

## Azure deployment

Full steps, secrets model, and Google OAuth origins:
**[deploy/azure/README.md](deploy/azure/README.md)**

Quick path:

```bash
az login
# Ensure .env has GOOGLE_CLIENT_ID, JWT_SECRET, GROK_API_KEY
./deploy/azure/deploy.sh
```

The script builds `linux/amd64` images, provisions/updates Container Apps,
Postgres, Redis, and Key Vault, then prints `WEB_URL` / `API_URL` and writes
`deploy/azure/.last-deploy.env` (gitignored).

After deploy:

1. Add `WEB_URL` to Google OAuth **Authorized JavaScript origins**
2. `curl "$API_URL/health"`
3. Open `WEB_URL` → sign in → analyze → **Download PDF**

Current live URLs (may change on redeploy):

| Service | URL |
|---------|-----|
| Web | https://ca-grok-web.thankfuldune-4d704de3.newzealandnorth.azurecontainerapps.io |
| API | https://ca-grok-api.thankfuldune-4d704de3.newzealandnorth.azurecontainerapps.io |

Tear down when finished:

```bash
az group delete --name rg-grok-career-coach --yes --no-wait
```

## Project structure

```text
src/GrokCareerCoach.Api/          ASP.NET Core API, Grok client, Redis cache
tests/GrokCareerCoach.Api.Tests/  xUnit tests
web/                              React + TypeScript UI (+ PDF export)
deploy/azure/                     Azure Container Apps deploy script + docs
docker-compose.yml                Local application stack
.github/workflows/ci.yml          Build and test
```

## Environment variables

Copy `.env.example` to `.env` for local overrides. Never commit real API keys or
production JWT secrets.

## CV / résumé blurb

> Shipped a live ASP.NET Core + React career-coaching app with Google JWT auth,
> PostgreSQL, Redis caching, optional xAI Grok, PDF export of coaching briefs,
> Docker, and Azure Container Apps + Key Vault.
> Live demo:
> https://ca-grok-web.thankfuldune-4d704de3.newzealandnorth.azurecontainerapps.io

## License

MIT
