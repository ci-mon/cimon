# Services
1. TC postgres localhost:5432
2. teamcity http://localhost:8112
3. vault http://localhost:8200
    dev user: u.cimon.dev/u.cimon.dev
    admin user admin/admin
4. jenkins http://localhost:8080
5. Git server gogs http://localhost:3001
    user: should be created manually. root/root

# Run separate
1. CI: `docker compose up db teamcity teamcity-agent-1 jenkins`
2. Vault: `docker compose up db teamcity teamcity-agent-1 jenkins`

# Test repository
Create repository with name `test1` and import [Project](TeamCity_GogsTest1.zip) into teamcity
Example commit: `Get-Date > 1.txt && git add . && git commit -m "Changes from $(Get-Date)" && git push`
Set user:
```
git config user.name "Test User"
git config user.email "test@example.com"
```