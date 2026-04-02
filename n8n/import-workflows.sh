#!/bin/bash
# =============================================================
# Auto-import n8n workflows via REST API
# Run this after `docker-compose up -d n8n` and n8n is ready
# Usage: bash n8n/import-workflows.sh
# =============================================================

N8N_URL="http://localhost:5678"
WORKFLOWS_DIR="$(dirname "$0")/workflows"

echo "⏳ Waiting for n8n to be ready..."
until curl -sf "$N8N_URL/healthz" > /dev/null 2>&1; do
  sleep 2
  echo "   ... still waiting"
done
echo "✅ n8n is up!"

# n8n v1 API requires a user to be set up first. 
# If n8n has N8N_BASIC_AUTH or owner setup, pass below:
N8N_EMAIL="${N8N_EMAIL:-admin@dataagent.local}"
N8N_PASSWORD="${N8N_PASSWORD:-Admin@12345}"

echo ""
echo "🔐 Setting up n8n owner account..."
curl -s -X POST "$N8N_URL/api/v1/owner/setup" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$N8N_EMAIL\",\"password\":\"$N8N_PASSWORD\",\"firstName\":\"DataAgent\",\"lastName\":\"Admin\"}" \
  > /dev/null 2>&1

echo "🔑 Getting API key..."
API_KEY=$(curl -s -X POST "$N8N_URL/api/v1/users/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$N8N_EMAIL\",\"password\":\"$N8N_PASSWORD\"}" | \
  grep -o '"token":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -z "$API_KEY" ]; then
  echo "⚠️  Could not get API key. Trying without auth (community edition)..."
  AUTH_HEADER=""
else
  AUTH_HEADER="-H \"Authorization: Bearer $API_KEY\""
  echo "✅ Got API token."
fi

echo ""
echo "📂 Importing workflows from: $WORKFLOWS_DIR"
for f in "$WORKFLOWS_DIR"/*.json; do
  name=$(basename "$f")
  echo "   📄 Importing: $name"
  
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$N8N_URL/api/v1/workflows" \
    -H "Content-Type: application/json" \
    ${API_KEY:+-H "Authorization: Bearer $API_KEY"} \
    -d @"$f")
  
  if [ "$STATUS" = "200" ] || [ "$STATUS" = "201" ]; then
    echo "   ✅ $name imported successfully (HTTP $STATUS)"
  else
    echo "   ⚠️  $name returned HTTP $STATUS — may already exist or need manual import"
  fi
done

echo ""
echo "🎉 Import done!"
echo "   Open n8n at: $N8N_URL"
echo "   Login: $N8N_EMAIL / $N8N_PASSWORD"
echo ""
echo "📋 NEXT STEPS:"
echo "   1. In n8n UI → Credentials → Create 'Header Auth' with:"
echo "      Name: N8N Header Auth"
echo "      Header Name: X-Auth-Token"
echo "      Header Value: \$(cat .env | grep N8N_AUTH_TOKEN | cut -d= -f2)"
echo ""
echo "   2. In n8n UI → Credentials → Create 'SMTP' with Gmail/SMTP settings"
echo ""
echo "   3. In n8n UI → Settings → Environment Variables → Add:"
echo "      GEMINI_API_KEY = your_gemini_api_key"
echo "      CALLBACK_HMAC_SECRET = SUPER_SECRET_HMAC_KEY_123456789012345"
