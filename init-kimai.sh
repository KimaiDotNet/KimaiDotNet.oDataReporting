#!/bin/sh
# Initialises the local Kimai test instance with a known admin user and API token.
# Runs once as the kimai-init service defined in docker-compose.yml.
#
# After this script completes the following are ready to use:
#   URL:   http://localhost:8001
#   User:  kimai-admin / Admin1234!
#   Token: kimai-local-integration-test-token
#
# To configure the OData service:
#   cd src/KimaiDotNet.Reporting.ODataService
#   dotnet user-secrets set "Kimai:Url" "http://localhost:8001"
#   dotnet user-secrets set "Kimai:Password" "kimai-local-integration-test-token"

set -e

CONSOLE="/opt/kimai/bin/console"
API_TOKEN="kimai-local-integration-test-token"

echo "==> Creating admin user..."
if $CONSOLE kimai:user:create kimai-admin admin@kimai.local ROLE_SUPER_ADMIN 'Admin1234!'; then
    echo "    User created."
else
    echo "    User already exists, resetting password..."
    $CONSOLE kimai:user:password kimai-admin -- 'Admin1234!'
fi

echo "==> Inserting API token directly into database..."
$CONSOLE dbal:run-sql \
    "INSERT INTO kimai2_access_token (user_id, token, name) \
     SELECT id, '${API_TOKEN}', 'integration-test' FROM kimai2_users \
     WHERE username = 'kimai-admin' \
     ON DUPLICATE KEY UPDATE token = '${API_TOKEN}'" \
    --no-interaction

echo ""
echo "============================================================"
echo "  Kimai test environment ready."
echo ""
echo "  URL:   http://localhost:8001"
echo "  User:  kimai-admin / Admin1234!"
echo "  Token: ${API_TOKEN}"
echo ""
echo "  Configure OData service:"
echo "    cd src/KimaiDotNet.Reporting.ODataService"
echo "    dotnet user-secrets set \"Kimai:Url\" \"http://localhost:8001\""
echo "    dotnet user-secrets set \"Kimai:Password\" \"${API_TOKEN}\""
echo "============================================================"
