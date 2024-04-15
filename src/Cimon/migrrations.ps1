dotnet tool install --global dotnet-ef
Remove-Item -Force -Recurse -Path "../Cimon.DB.Migrations.Sqlite/Migrations"
Remove-Item -Force -Recurse -Path "../Cimon.DB.Migrations.SqlServer/Migrations"
dotnet ef migrations add Initial --project ../Cimon.DB.Migrations.Sqlite  -- --DbProvider Sqlite
dotnet ef migrations add Initial --project ../Cimon.DB.Migrations.SqlServer  -- --DbProvider SqlServer
#dotnet ef migrations remove --context CimonDbContext --project ../Cimon.DB.Migrations.Sqlite  -- --DbProvider Sqlite
dotnet ef migrations add monitor_groups --context CimonDbContext --project ../Cimon.DB.Migrations.Sqlite  -- --DbProvider Sqlite
git add ../Cimon.DB.Migrations.Sqlite/*
#dotnet ef migrations remove --context CimonDbContext --project ../Cimon.DB.Migrations.SqlServer  -- --DbProvider SqlServer
dotnet ef migrations add monitor_groups --context CimonDbContext --project ../Cimon.DB.Migrations.SqlServer  -- --DbProvider SqlServer
git add ../Cimon.DB.Migrations.SqlServer/*

dotnet ef database update --context CimonDbContext --project ../Cimon.DB.Migrations.Sqlite  -- --DbProvider Sqlite