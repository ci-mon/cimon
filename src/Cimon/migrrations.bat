rmdir /S /Q "../Cimon.DB.Migrations.Sqlite/Migrations"
rmdir /S /Q "../Cimon.DB.Migrations.SqlServer/Migrations"
dotnet ef migrations add Initial --project ../Cimon.DB.Migrations.Sqlite  -- --DbProvider Sqlite
dotnet ef migrations add Initial --project ../Cimon.DB.Migrations.SqlServer  -- --DbProvider SqlServer
