# dev
type .\secrets.local.json | dotnet user-secrets set

# prod
#setx CIMON_Secrets__Vault__Url "http://localhost:8200" /M
#setx CIMON_Secrets__Vault__Token "root" /M
#setx CIMON_Secrets__Vault__Path "dev" /M

