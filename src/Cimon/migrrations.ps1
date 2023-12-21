dotnet tool install --global dotnet-ef
Remove-Item -Force -Recurse -Path "../Cimon.DB.Migrations.Sqlite/Migrations"
Remove-Item -Force -Recurse -Path "../Cimon.DB.Migrations.SqlServer/Migrations"
dotnet ef migrations add Initial --project ../Cimon.DB.Migrations.Sqlite  -- --DbProvider Sqlite
dotnet ef migrations add Initial --project ../Cimon.DB.Migrations.SqlServer  -- --DbProvider SqlServer
