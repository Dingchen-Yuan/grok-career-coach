#!/usr/bin/env bash
# Provision Azure resources and deploy grok-career-coach (API + Web).
# Prerequisites: az login, Docker, and a filled root .env
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

if [[ ! -f .env ]]; then
  echo "Missing .env — copy .env.example and set secrets first." >&2
  exit 1
fi

# shellcheck disable=SC1091
set -a
source .env
set +a

: "${GOOGLE_CLIENT_ID:?Set GOOGLE_CLIENT_ID in .env}"
: "${JWT_SECRET:?Set JWT_SECRET in .env}"
: "${GROK_API_KEY:?Set GROK_API_KEY in .env for production Grok}"

LOCATION="${AZURE_LOCATION:-newzealandnorth}"
RG="${AZURE_RESOURCE_GROUP:-rg-grok-career-coach}"
ACR_NAME="${AZURE_ACR_NAME:-grokcareercoachacr}"
ENV_NAME="${AZURE_CONTAINER_ENV:-env-grok-career-coach}"
API_APP="${AZURE_API_APP:-ca-grok-api}"
WEB_APP="${AZURE_WEB_APP:-ca-grok-web}"
KV_NAME="${AZURE_KEY_VAULT_NAME:-kv-grok-career}"
PG_SERVER="${AZURE_PG_SERVER:-pg-grok-career}"
PG_ADMIN_USER="${AZURE_PG_ADMIN_USER:-grokadmin}"
PG_ADMIN_PASSWORD="${AZURE_PG_ADMIN_PASSWORD:-$(openssl rand -base64 24 | tr -d '/+=' | head -c 24)Aa1}"
PG_DB="${POSTGRES_DB:-grok_career_coach}"
REDIS_NAME="${AZURE_REDIS_NAME:-redis-grok-career}"
LOG_WORKSPACE="${AZURE_LOG_WORKSPACE:-law-grok-career}"

if ! az account show >/dev/null 2>&1; then
  echo "Not logged in. Run: az login" >&2
  exit 1
fi

ACR_NAME="$(echo "$ACR_NAME" | tr '[:upper:]' '[:lower:]' | tr -cd 'a-z0-9')"
KV_NAME="$(echo "$KV_NAME" | tr '[:upper:]' '[:lower:]' | cut -c1-24)"

echo "==> Registering resource providers (may take a few minutes the first time)"
for ns in \
  Microsoft.ContainerRegistry \
  Microsoft.App \
  Microsoft.DBforPostgreSQL \
  Microsoft.Cache \
  Microsoft.KeyVault \
  Microsoft.OperationalInsights \
  Microsoft.Network
do
  state="$(az provider show -n "$ns" --query registrationState -o tsv 2>/dev/null || echo NotRegistered)"
  if [[ "$state" != "Registered" ]]; then
    echo "  registering $ns ..."
    az provider register --namespace "$ns" --wait --output none
  fi
done

echo "==> Resource group: $RG ($LOCATION)"
az group create --name "$RG" --location "$LOCATION" --output none

echo "==> Log Analytics workspace"
if ! az monitor log-analytics workspace show \
  --resource-group "$RG" \
  --workspace-name "$LOG_WORKSPACE" >/dev/null 2>&1; then
  az monitor log-analytics workspace create \
    --resource-group "$RG" \
    --workspace-name "$LOG_WORKSPACE" \
    --location "$LOCATION" \
    --output none
fi
LAW_ID="$(az monitor log-analytics workspace show \
  --resource-group "$RG" \
  --workspace-name "$LOG_WORKSPACE" \
  --query customerId -o tsv)"
LAW_KEY="$(az monitor log-analytics workspace get-shared-keys \
  --resource-group "$RG" \
  --workspace-name "$LOG_WORKSPACE" \
  --query primarySharedKey -o tsv)"

echo "==> Container Registry: $ACR_NAME"
if ! az acr show --name "$ACR_NAME" >/dev/null 2>&1; then
  az acr create \
    --resource-group "$RG" \
    --name "$ACR_NAME" \
    --sku Basic \
    --admin-enabled true \
    --location "$LOCATION" \
    --output none
fi
ACR_LOGIN_SERVER="$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)"
ACR_USER="$(az acr credential show --name "$ACR_NAME" --query username -o tsv)"
ACR_PASS="$(az acr credential show --name "$ACR_NAME" --query 'passwords[0].value' -o tsv)"

echo "==> PostgreSQL Flexible Server: $PG_SERVER"
if ! az postgres flexible-server show --resource-group "$RG" --name "$PG_SERVER" >/dev/null 2>&1; then
  az postgres flexible-server create \
    --resource-group "$RG" \
    --name "$PG_SERVER" \
    --location "$LOCATION" \
    --admin-user "$PG_ADMIN_USER" \
    --admin-password "$PG_ADMIN_PASSWORD" \
    --sku-name Standard_B1ms \
    --tier Burstable \
    --storage-size 32 \
    --version 16 \
    --public-access 0.0.0.0-255.255.255.255 \
    --yes \
    --output none
fi
az postgres flexible-server db create \
  --resource-group "$RG" \
  --server-name "$PG_SERVER" \
  --database-name "$PG_DB" \
  --output none 2>/dev/null || true
PG_FQDN="$(az postgres flexible-server show \
  --resource-group "$RG" \
  --name "$PG_SERVER" \
  --query fullyQualifiedDomainName -o tsv)"
POSTGRES_CONN="Host=${PG_FQDN};Port=5432;Database=${PG_DB};Username=${PG_ADMIN_USER};Password=${PG_ADMIN_PASSWORD};Ssl Mode=Require"

echo "==> Azure Cache for Redis: $REDIS_NAME"
if ! az redis show --resource-group "$RG" --name "$REDIS_NAME" >/dev/null 2>&1; then
  az redis create \
    --resource-group "$RG" \
    --name "$REDIS_NAME" \
    --location "$LOCATION" \
    --sku Basic \
    --vm-size c0 \
    --output none
fi
REDIS_HOST="$(az redis show --resource-group "$RG" --name "$REDIS_NAME" --query hostName -o tsv)"
REDIS_KEY="$(az redis list-keys --resource-group "$RG" --name "$REDIS_NAME" --query primaryKey -o tsv)"
REDIS_CONN="${REDIS_HOST}:6380,password=${REDIS_KEY},ssl=True,abortConnect=False"

echo "==> Key Vault: $KV_NAME"
if ! az keyvault show --name "$KV_NAME" >/dev/null 2>&1; then
  az keyvault create \
    --resource-group "$RG" \
    --name "$KV_NAME" \
    --location "$LOCATION" \
    --enable-rbac-authorization false \
    --output none
fi
KV_URI="$(az keyvault show --name "$KV_NAME" --query properties.vaultUri -o tsv)"

set_secret() {
  local name="$1"
  local value="$2"
  az keyvault secret set --vault-name "$KV_NAME" --name "$name" --value "$value" --output none
}

echo "==> Writing secrets to Key Vault"
set_secret "Google--ClientId" "$GOOGLE_CLIENT_ID"
set_secret "Jwt--Secret" "$JWT_SECRET"
set_secret "Jwt--Issuer" "GrokCareerCoach"
set_secret "Jwt--Audience" "GrokCareerCoach.Web"
set_secret "Grok--ApiKey" "$GROK_API_KEY"
set_secret "ConnectionStrings--Postgres" "$POSTGRES_CONN"
set_secret "ConnectionStrings--Redis" "$REDIS_CONN"

echo "==> Container Apps environment"
az containerapp env create \
  --name "$ENV_NAME" \
  --resource-group "$RG" \
  --location "$LOCATION" \
  --logs-workspace-id "$LAW_ID" \
  --logs-workspace-key "$LAW_KEY" \
  --output none 2>/dev/null || true

echo "==> Building and pushing images"
echo "$ACR_PASS" | docker login "$ACR_LOGIN_SERVER" -u "$ACR_USER" --password-stdin

docker build \
  --platform linux/amd64 \
  -f src/GrokCareerCoach.Api/Dockerfile \
  -t "$ACR_LOGIN_SERVER/api:latest" \
  .
docker push "$ACR_LOGIN_SERVER/api:latest"

# API URL is deterministic once the app exists; create API first without CORS web URL,
# then rebuild web with the real API hostname.
API_FQDN_PLACEHOLDER="https://placeholder.invalid"

deploy_or_update_api() {
  local cors_origin="$1"
  local env_vars=(
    "ASPNETCORE_ENVIRONMENT=Production"
    "KEY_VAULT_URI=$KV_URI"
    "Cors__AllowedOriginsCsv=$cors_origin"
    "ConnectionStrings__Postgres=$POSTGRES_CONN"
    "ConnectionStrings__Redis=$REDIS_CONN"
    "Google__ClientId=$GOOGLE_CLIENT_ID"
    "Jwt__Secret=$JWT_SECRET"
    "Jwt__Issuer=GrokCareerCoach"
    "Jwt__Audience=GrokCareerCoach.Web"
    "Grok__ApiKey=$GROK_API_KEY"
  )
  if az containerapp show --name "$API_APP" --resource-group "$RG" >/dev/null 2>&1; then
    az containerapp update \
      --name "$API_APP" \
      --resource-group "$RG" \
      --image "$ACR_LOGIN_SERVER/api:latest" \
      --set-env-vars "${env_vars[@]}" \
      --output none
  else
    az containerapp create \
      --name "$API_APP" \
      --resource-group "$RG" \
      --environment "$ENV_NAME" \
      --image "$ACR_LOGIN_SERVER/api:latest" \
      --registry-server "$ACR_LOGIN_SERVER" \
      --registry-username "$ACR_USER" \
      --registry-password "$ACR_PASS" \
      --target-port 8080 \
      --ingress external \
      --min-replicas 1 \
      --max-replicas 2 \
      --cpu 0.5 \
      --memory 1.0Gi \
      --system-assigned \
      --env-vars "${env_vars[@]}" \
      --output none
  fi
}

echo "==> Creating API Container App (temporary CORS)"
deploy_or_update_api "http://localhost:5173"

API_FQDN="$(az containerapp show --name "$API_APP" --resource-group "$RG" --query properties.configuration.ingress.fqdn -o tsv)"
API_URL="https://${API_FQDN}"

echo "==> Granting API identity access to Key Vault"
API_PRINCIPAL="$(az containerapp show \
  --name "$API_APP" \
  --resource-group "$RG" \
  --query identity.principalId -o tsv)"
az keyvault set-policy \
  --name "$KV_NAME" \
  --object-id "$API_PRINCIPAL" \
  --secret-permissions get list \
  --output none

# Restart so the first boot that could read Key Vault applies migrations.
API_REVISION="$(az containerapp revision list \
  --name "$API_APP" \
  --resource-group "$RG" \
  --query '[0].name' -o tsv)"
az containerapp revision restart \
  --name "$API_APP" \
  --resource-group "$RG" \
  --revision "$API_REVISION" \
  --output none 2>/dev/null || true

echo "==> Building web image against $API_URL"
docker build \
  --platform linux/amd64 \
  -f web/Dockerfile \
  --build-arg "VITE_API_BASE_URL=$API_URL" \
  --build-arg "VITE_GOOGLE_CLIENT_ID=$GOOGLE_CLIENT_ID" \
  -t "$ACR_LOGIN_SERVER/web:latest" \
  .
docker push "$ACR_LOGIN_SERVER/web:latest"

echo "==> Creating / updating Web Container App"
if az containerapp show --name "$WEB_APP" --resource-group "$RG" >/dev/null 2>&1; then
  az containerapp update \
    --name "$WEB_APP" \
    --resource-group "$RG" \
    --image "$ACR_LOGIN_SERVER/web:latest" \
    --output none
else
  az containerapp create \
    --name "$WEB_APP" \
    --resource-group "$RG" \
    --environment "$ENV_NAME" \
    --image "$ACR_LOGIN_SERVER/web:latest" \
    --registry-server "$ACR_LOGIN_SERVER" \
    --registry-username "$ACR_USER" \
    --registry-password "$ACR_PASS" \
    --target-port 80 \
    --ingress external \
    --min-replicas 1 \
    --max-replicas 2 \
    --cpu 0.25 \
    --memory 0.5Gi \
    --output none
fi

WEB_FQDN="$(az containerapp show --name "$WEB_APP" --resource-group "$RG" --query properties.configuration.ingress.fqdn -o tsv)"
WEB_URL="https://${WEB_FQDN}"

echo "==> Updating API CORS for production web origin"
deploy_or_update_api "$WEB_URL"

# Restart API so Key Vault secrets + CORS are picked up cleanly
az containerapp revision restart \
  --name "$API_APP" \
  --resource-group "$RG" \
  --revision "$(az containerapp revision list --name "$API_APP" --resource-group "$RG" --query '[0].name' -o tsv)" \
  --output none 2>/dev/null || true

OUT_FILE="$ROOT_DIR/deploy/azure/.last-deploy.env"
cat >"$OUT_FILE" <<EOF
AZURE_RESOURCE_GROUP=$RG
AZURE_LOCATION=$LOCATION
WEB_URL=$WEB_URL
API_URL=$API_URL
KEY_VAULT_URI=$KV_URI
GOOGLE_JS_ORIGIN=$WEB_URL
EOF

echo
echo "Deploy finished."
echo "  Web:  $WEB_URL"
echo "  API:  $API_URL/health"
echo "  Vault: $KV_URI"
echo
echo "Next: add this Authorized JavaScript origin in Google Cloud Console:"
echo "  $WEB_URL"
echo
echo "URLs also written to deploy/azure/.last-deploy.env (gitignored)."
echo "To tear down later: az group delete --name $RG --yes --no-wait"
