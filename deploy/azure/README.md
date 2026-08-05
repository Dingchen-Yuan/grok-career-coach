# Azure deployment

Deploy the Grok Career Coach stack to **Azure Container Apps** with:

- Azure Container Registry (API + Web images)
- Azure Database for PostgreSQL Flexible Server
- Azure Cache for Redis
- Azure Key Vault (Google / JWT / Grok / connection strings)
- System-assigned managed identity on the API to read Key Vault

## Prerequisites

1. [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) and Docker Desktop
2. An Azure subscription
3. Root [`.env`](../../.env) with at least:

```bash
GOOGLE_CLIENT_ID=....apps.googleusercontent.com
JWT_SECRET=long-random-string
GROK_API_KEY=xai-...
```

Optional overrides (Azure for Students on this account: use `newzealandnorth`;
`eastus` / `australiaeast` are blocked by policy):

```bash
AZURE_LOCATION=newzealandnorth
AZURE_RESOURCE_GROUP=rg-grok-career-coach
AZURE_ACR_NAME=grokcareercoachacr
AZURE_KEY_VAULT_NAME=kv-grok-career
```

Also build images with `--platform linux/amd64` so Mac ARM builds run on Azure.

The script prints `WEB_URL` and `API_URL`, and writes them to
`deploy/azure/.last-deploy.env` (gitignored).

## Google OAuth (required after first deploy)

1. Open [Google Auth Platform → Clients](https://console.cloud.google.com/auth/clients)
2. Edit your Web client
3. Under **Authorized JavaScript origins**, add the printed `WEB_URL`
   (example: `https://ca-grok-web.<region>.azurecontainerapps.io`)
4. Keep `http://localhost:5173` for local development
5. Wait a few minutes, then open `WEB_URL` and sign in

## Verify

```bash
source deploy/azure/.last-deploy.env
curl -sS "$API_URL/health"
# open $WEB_URL → Google sign-in → submit JD + résumé
# confirm the brief is not the mock “deterministic” style when GROK_API_KEY is set
```

## Secrets model

| Secret name in Key Vault | App setting |
|--------------------------|-------------|
| `Google--ClientId` | `Google:ClientId` |
| `Jwt--Secret` | `Jwt:Secret` |
| `Grok--ApiKey` | `Grok:ApiKey` |
| `ConnectionStrings--Postgres` | `ConnectionStrings:Postgres` |
| `ConnectionStrings--Redis` | `ConnectionStrings:Redis` |

The API loads Key Vault when `KEY_VAULT_URI` is set (managed identity +
`DefaultAzureCredential`). Do **not** commit `.env` or put secrets in images.

## Tear down (stop billing)

```bash
az group delete --name rg-grok-career-coach --yes --no-wait
```
