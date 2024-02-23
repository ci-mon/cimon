[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=ci-mon_cimon&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=ci-mon_cimon)

# Cimon
Collaborative continuous integration monitoring tool.
Web application and desktop client. Useful for trunk-based development when your teammate can easily brake CI. 

## Features
1. [x] monitor builds: see status, get notified about status changes
2. [x] teamcity and jenkins support
3. [x] collaborate: discuss failure reasons, ask for fixes
4. [x] smart blame
5. [x] LDAP auth
6. [ ] quick actions (mute, investigate)

## Development
Web app located in `/src/Cimon`.
Web app use sqlite db in development mode and creates some demo data during db initialization.

### Secrets
To connect to real services you can use docker compose file in `infrastructure` dir.
To use default auth data for local services you can run inside `src/Cimon` dir
```
type .\secrets.json | dotnet user-secrets set
```
To use production auth data copy `.\secrets.json` to `.\secrets.local.json` and set data there (this file ignored by git so will not be committed).
```
type .\secrets.local.json | dotnet user-secrets set
```
Best way to connect to real services for development is to setup `vault` server, initialize it as 
`vault kv put infrastructure.cimon/dev @secrets.local.json`, example here `infrastructure/vault-client`

### DB structure changes
Ensure that changes are compatible with existing data. Create migrations for both projects:
```
dotnet ef migrations add migration_name --startup-project Cimon/Cimon.csproj --project Cimon.DB.Migrations.Sqlite  -- --DbProvider Sqlite
dotnet ef migrations add migration_name --startup-project Cimon/Cimon.csproj --project Cimon.DB.Migrations.SqlServer  -- --DbProvider SqlServer
```

### e2e tests
To run e2e tests: 
```
cd e2e
npm i
npm test
```

### Desktop app
Desktop app located in `src/cimon-desktop` folder.
To publish new version:
1. Set new version in [package.json](src/cimon-desktop/package.json)
2. Ensure [CHANGELOG.md](src/cimon-desktop/CHANGELOG.md) is actualized
3. Set cimon web app url in [cimon-config.ts](src/cimon-desktop/cimon-config.ts)
4. Run ```npm run publish```. This command will build app and publish it to web app, this will trigger update check.

## Info

[![SonarCloud](https://sonarcloud.io/images/project_badges/sonarcloud-orange.svg)](https://sonarcloud.io/summary/new_code?id=ci-mon_cimon)
