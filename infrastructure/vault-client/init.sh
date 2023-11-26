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
vault kv put infrastructure.cimon/dev teamcity.teamcity_main.uri=http://localhost:8112
vault kv patch infrastructure.cimon/dev teamcity.teamcity_main.login=admin
vault kv patch infrastructure.cimon/dev teamcity.teamcity_main.password=admin
vault kv patch infrastructure.cimon/dev jenkins.jenkins_main.uri=http://localhost:8080
vault kv patch infrastructure.cimon/dev jenkins.jenkins_main.login=admin
vault kv patch infrastructure.cimon/dev jenkins.jenkins_main.token=11338fd16b8c7c51052d933d9f265ce528
vault kv patch infrastructure.cimon/dev ldap_client.host=dc.com
echo "Complete..."
