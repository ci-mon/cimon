﻿// <auto-generated />
using System;
using Cimon.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Cimon.DB.Migrations.SqlServer.Migrations
{
    [DbContext(typeof(CimonDbContext))]
    partial class CimonDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Cimon.DB.Models.AppFeatureState", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Enabled")
                        .HasColumnType("bit");

                    b.Property<int?>("TeamId")
                        .HasColumnType("int");

                    b.Property<int?>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("TeamId");

                    b.HasIndex("UserId");

                    b.ToTable("FeatureStates");
                });

            modelBuilder.Entity("Cimon.DB.Models.BuildConfigModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("AllowML")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("Branch")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("ConnectorId")
                        .HasColumnType("int");

                    b.Property<string>("DemoState")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDefaultBranch")
                        .HasColumnType("bit");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Props")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ConnectorId", "Id", "Branch");

                    b.ToTable("BuildConfigurations");
                });

            modelBuilder.Entity("Cimon.DB.Models.BuildInMonitor", b =>
                {
                    b.Property<int>("MonitorId")
                        .HasColumnType("int");

                    b.Property<int>("BuildConfigId")
                        .HasColumnType("int");

                    b.HasKey("MonitorId", "BuildConfigId");

                    b.HasIndex("BuildConfigId");

                    b.ToTable("MonitorBuilds");
                });

            modelBuilder.Entity("Cimon.DB.Models.CIConnector", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CISystem")
                        .HasColumnType("int");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("CIConnectors");
                });

            modelBuilder.Entity("Cimon.DB.Models.CIConnectorSetting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CIConnectorId")
                        .HasColumnType("int");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CIConnectorId");

                    b.ToTable("CIConnectorSettings");
                });

            modelBuilder.Entity("Cimon.DB.Models.ConnectedMonitor", b =>
                {
                    b.Property<int>("SourceMonitorModelId")
                        .HasColumnType("int");

                    b.Property<int>("ConnectedMonitorModelId")
                        .HasColumnType("int");

                    b.HasKey("SourceMonitorModelId", "ConnectedMonitorModelId");

                    b.HasIndex("ConnectedMonitorModelId");

                    b.ToTable("ConnectedMonitors");
                });

            modelBuilder.Entity("Cimon.DB.Models.MonitorModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("AlwaysOnMonitoring")
                        .HasColumnType("bit");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("OwnerId")
                        .HasColumnType("int");

                    b.Property<bool>("Removed")
                        .HasColumnType("bit");

                    b.Property<bool>("Shared")
                        .HasColumnType("bit");

                    b.Property<string>("Title")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<string>("ViewSettings")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("OwnerId");

                    b.ToTable("Monitors");
                });

            modelBuilder.Entity("Cimon.DB.Models.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("Cimon.DB.Models.Team", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Teams");
                });

            modelBuilder.Entity("Cimon.DB.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("AllowLocalLogin")
                        .HasColumnType("bit");

                    b.Property<string>("DefaultMonitorId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDeactivated")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("RoleRole", b =>
                {
                    b.Property<int>("OwnedRolesId")
                        .HasColumnType("int");

                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.HasKey("OwnedRolesId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("RoleRole");
                });

            modelBuilder.Entity("RoleUser", b =>
                {
                    b.Property<int>("RolesId")
                        .HasColumnType("int");

                    b.Property<int>("UsersId")
                        .HasColumnType("int");

                    b.HasKey("RolesId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("RoleUser");
                });

            modelBuilder.Entity("TeamTeam", b =>
                {
                    b.Property<int>("ChildTeamsId")
                        .HasColumnType("int");

                    b.Property<int>("TeamId")
                        .HasColumnType("int");

                    b.HasKey("ChildTeamsId", "TeamId");

                    b.HasIndex("TeamId");

                    b.ToTable("TeamTeam");
                });

            modelBuilder.Entity("TeamUser", b =>
                {
                    b.Property<int>("TeamsId")
                        .HasColumnType("int");

                    b.Property<int>("UsersId")
                        .HasColumnType("int");

                    b.HasKey("TeamsId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("TeamUser");
                });

            modelBuilder.Entity("Cimon.DB.Models.AppFeatureState", b =>
                {
                    b.HasOne("Cimon.DB.Models.Team", "Team")
                        .WithMany()
                        .HasForeignKey("TeamId");

                    b.HasOne("Cimon.DB.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");

                    b.Navigation("Team");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Cimon.DB.Models.BuildConfigModel", b =>
                {
                    b.HasOne("Cimon.DB.Models.CIConnector", "Connector")
                        .WithMany("BuildConfigModels")
                        .HasForeignKey("ConnectorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Connector");
                });

            modelBuilder.Entity("Cimon.DB.Models.BuildInMonitor", b =>
                {
                    b.HasOne("Cimon.DB.Models.BuildConfigModel", "BuildConfig")
                        .WithMany("Monitors")
                        .HasForeignKey("BuildConfigId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Cimon.DB.Models.MonitorModel", "Monitor")
                        .WithMany("Builds")
                        .HasForeignKey("MonitorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BuildConfig");

                    b.Navigation("Monitor");
                });

            modelBuilder.Entity("Cimon.DB.Models.CIConnectorSetting", b =>
                {
                    b.HasOne("Cimon.DB.Models.CIConnector", "CIConnector")
                        .WithMany()
                        .HasForeignKey("CIConnectorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CIConnector");
                });

            modelBuilder.Entity("Cimon.DB.Models.ConnectedMonitor", b =>
                {
                    b.HasOne("Cimon.DB.Models.MonitorModel", "ConnectedMonitorModel")
                        .WithMany()
                        .HasForeignKey("ConnectedMonitorModelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Cimon.DB.Models.MonitorModel", "SourceMonitorModel")
                        .WithMany("ConnectedMonitors")
                        .HasForeignKey("SourceMonitorModelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ConnectedMonitorModel");

                    b.Navigation("SourceMonitorModel");
                });

            modelBuilder.Entity("Cimon.DB.Models.MonitorModel", b =>
                {
                    b.HasOne("Cimon.DB.Models.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId");

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("RoleRole", b =>
                {
                    b.HasOne("Cimon.DB.Models.Role", null)
                        .WithMany()
                        .HasForeignKey("OwnedRolesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Cimon.DB.Models.Role", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientCascade)
                        .IsRequired();
                });

            modelBuilder.Entity("RoleUser", b =>
                {
                    b.HasOne("Cimon.DB.Models.Role", null)
                        .WithMany()
                        .HasForeignKey("RolesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Cimon.DB.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TeamTeam", b =>
                {
                    b.HasOne("Cimon.DB.Models.Team", null)
                        .WithMany()
                        .HasForeignKey("ChildTeamsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Cimon.DB.Models.Team", null)
                        .WithMany()
                        .HasForeignKey("TeamId")
                        .OnDelete(DeleteBehavior.ClientCascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TeamUser", b =>
                {
                    b.HasOne("Cimon.DB.Models.Team", null)
                        .WithMany()
                        .HasForeignKey("TeamsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Cimon.DB.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Cimon.DB.Models.BuildConfigModel", b =>
                {
                    b.Navigation("Monitors");
                });

            modelBuilder.Entity("Cimon.DB.Models.CIConnector", b =>
                {
                    b.Navigation("BuildConfigModels");
                });

            modelBuilder.Entity("Cimon.DB.Models.MonitorModel", b =>
                {
                    b.Navigation("Builds");

                    b.Navigation("ConnectedMonitors");
                });
#pragma warning restore 612, 618
        }
    }
}
