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
echo "Adding users..."
vault write auth/userpass/users/u.cimon.dev password=u.cimon.dev
vault write auth/userpass/users/admin password=admin
echo "Creating entity..."
ADMIN_ENTITY_ID=$(vault write -format=json identity/entity name="admin" | jq -r '.data.id')
DEV_ENTITY_ID=$(vault write -format=json identity/entity name="u.cimon.dev" | jq -r '.data.id')
echo "Associating entity with user..."
vault write identity/entity-alias name="admin" canonical_id=$ADMIN_ENTITY_ID mount_accessor=$(vault auth list -format=json | jq -r '.["userpass/"].accessor')
vault write identity/entity-alias name="u.cimon.dev" canonical_id=$DEV_ENTITY_ID mount_accessor=$(vault auth list -format=json | jq -r '.["userpass/"].accessor')

echo "Creating ACL..."
vault policy write p.admin - <<EOF
path "*" {
  capabilities = ["create", "update", "patch", "read", "delete", "list"]
}
EOF
vault policy write p.cimon.dev - <<EOF
path "infrastructure.cimon/+/dev" {
  capabilities = ["create", "update", "patch", "read", "delete", "list"]
}
EOF
echo "Associating ACL with the entities..."
vault write identity/entity/name/admin policies=p.admin
vault write identity/entity/name/u.cimon.dev policies=p.cimon.dev
echo "Adding entries..."
vault kv put infrastructure.cimon/dev teamcity.user=admin
vault kv patch infrastructure.cimon/dev teamcity.password=admin

echo "Complete..."
