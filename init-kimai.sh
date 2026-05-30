#!/bin/sh
# Initialises the local Kimai test instance with a known admin user.
# Runs once as the kimai-init service defined in docker-compose.yml.
#
# After this script completes:
#   1. Log in at http://localhost:8001 as kimai-admin / Admin1234!
#   2. Navigate to your profile → API tokens → create a token
#   3. Store the token in user secrets:
#        cd src/KimaiDotNet.Reporting.ODataService
#        dotnet user-secrets set "Kimai:Url" "http://localhost:8001"
#        dotnet user-secrets set "Kimai:Password" "<your-api-token>"

set -e

CONSOLE="/opt/kimai/bin/console"

echo "==> Creating admin user..."
if $CONSOLE kimai:user:create kimai-admin admin@kimai.local ROLE_SUPER_ADMIN; then
    echo "    User created."
else
    echo "    User already exists, continuing."
fi

echo "==> Setting admin password..."
$CONSOLE kimai:user:password kimai-admin -- 'Admin1234!'

# kimai:api-token:create was added in Kimai 2.x — try it and capture the output.
echo "==> Attempting to create API token via console..."
if TOKEN=$($CONSOLE kimai:api-token:create kimai-admin 'integration-test' 2>&1); then
    # Extract the token value from the command output (it is printed on its own line)
    RAW=$(echo "$TOKEN" | grep -Eo '[A-Za-z0-9_\-]{20,}' | tail -1)
    if [ -n "$RAW" ]; then
        echo ""
        echo "============================================================"
        echo "  API token created successfully!"
        echo ""
        echo "  Run the following to configure the OData service:"
        echo ""
        echo "    cd src/KimaiDotNet.Reporting.ODataService"
        echo "    dotnet user-secrets set \"Kimai:Url\" \"http://localhost:8001\""
        echo "    dotnet user-secrets set \"Kimai:Password\" \"$RAW\""
        echo "============================================================"
    else
        echo "  Token command ran but token value could not be parsed."
        echo "  Create a token manually in the Kimai web UI."
    fi
else
    echo ""
    echo "============================================================"
    echo "  kimai:api-token:create is not available in this build."
    echo "  Create the token manually:"
    echo "    1. Open http://localhost:8001"
    echo "    2. Log in as kimai-admin / Admin1234!"
    echo "    3. Profile → API tokens → create a token"
    echo "    4. dotnet user-secrets set \"Kimai:Password\" \"<token>\""
    echo "============================================================"
fi
