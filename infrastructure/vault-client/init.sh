#!/bin/bash

VAULT_RETRIES=5
echo "Vault is starting..."
until vault status > /dev/null 2>&1 || [ "$VAULT_RETRIES" -eq 0 ]; do
        echo "Waiting for vault to start...: $((VAULT_RETRIES--))"
        sleep 1
done
echo "Authenticating to vault... token=$VAULT_DEV_ROOT_TOKEN_ID"
vault login token=$VAULT_DEV_ROOT_TOKEN_ID
echo "Initializing vault..."
vault secrets enable -version=2 -path=infrastructure.cimon kv
vault auth enable userpass
echo "Creating ACL..."
vault policy write p.cimon.dev - <<EOF
path "infrastructure.cimon/+/dev" {
  capabilities = ["create", "update", "patch", "read", "delete", "list"]
}
EOF
echo "Creating tokens..."
vault token create -period=180m -policy=p.cimon.dev -id=10000000-0000-0000-0000-000000000000
vault token create -period=180m -policy=root -id=00000000-0000-0000-0000-000000000000
echo "Adding entries..."
vault kv put infrastructure.cimon/dev @secrets.json
echo "Complete..."
