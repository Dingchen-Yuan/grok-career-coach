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
| Auth | JWT (access + refresh) |
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
- [ ] User registration / login with JWT
- [ ] Persist users & coaching sessions in PostgreSQL
- [ ] Grok-backed “JD × résumé” analysis endpoint
- [ ] Redis rate limiting / short-lived response cache
- [ ] Docker Compose for local API + Postgres + Redis
- [ ] Swagger + automated tests (Grok mocked)
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

> Implementation is in progress. Commands below will apply once the solution is added.

```bash
# coming soon
docker compose up --build
```

## Environment variables (planned)

See `.env.example` once the API scaffold lands. Never commit real API keys.

## CV / résumé blurb

> Building an ASP.NET Core career-coaching API with JWT, PostgreSQL, Redis, Docker, and xAI Grok, targeting deployment on Azure (App Service + Key Vault).

## License

MIT
