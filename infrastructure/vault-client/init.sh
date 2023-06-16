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
echo "Adding entries..."
vault kv put infrastructure.cimon/dev teamcity.user=admin
vault kv patch infrastructure.cimon/dev teamcity.password=admin
echo "Complete..."
