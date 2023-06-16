# Services
1. TC postgres localhost:5432
2. teamcity http://localhost:8112
3. vault http://localhost:8200
    dev user: u.cimon.dev/u.cimon.dev
    admin user admin/admin
4. jenkins http://localhost:8080

# Run separate
1. CI: `docker compose up db teamcity teamcity-agent-1 jenkins`
2. Vault: `docker compose up db teamcity teamcity-agent-1 jenkins`