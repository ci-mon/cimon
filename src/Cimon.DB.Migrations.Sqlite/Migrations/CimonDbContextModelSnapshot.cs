﻿// <auto-generated />
using Cimon.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Cimon.DB.Migrations.Sqlite.Migrations
{
    [DbContext(typeof(CimonDbContext))]
    partial class CimonDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Cimon.DB.Models.BuildConfigModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Branch")
                        .HasColumnType("TEXT");

                    b.Property<int>("ConnectorId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DemoState")
                        .HasColumnType("jsonb");

                    b.Property<bool>("IsDefaultBranch")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Props")
                        .HasColumnType("jsonb");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ConnectorId", "Id", "Branch");

                    b.ToTable("BuildConfigurations");
                });

            modelBuilder.Entity("Cimon.DB.Models.BuildInMonitor", b =>
                {
                    b.Property<int>("MonitorId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BuildConfigId")
                        .HasColumnType("INTEGER");

                    b.HasKey("MonitorId", "BuildConfigId");

                    b.HasIndex("BuildConfigId");

                    b.ToTable("MonitorBuilds");
                });

            modelBuilder.Entity("Cimon.DB.Models.CIConnector", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CISystem")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("CIConnectors");
                });

            modelBuilder.Entity("Cimon.DB.Models.CIConnectorSetting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CIConnectorId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CIConnectorId");

                    b.ToTable("CIConnectorSettings");
                });

            modelBuilder.Entity("Cimon.DB.Models.MonitorModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AlwaysOnMonitoring")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Removed")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Shared")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Monitors");
                });

            modelBuilder.Entity("Cimon.DB.Models.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("Cimon.DB.Models.Team", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Teams");
                });

            modelBuilder.Entity("Cimon.DB.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AllowLocalLogin")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DefaultMonitorId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .HasColumnType("TEXT");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeactivated")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("RoleRole", b =>
                {
                    b.Property<int>("OwnedRolesId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RoleId")
                        .HasColumnType("INTEGER");

                    b.HasKey("OwnedRolesId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("RoleRole");
                });

            modelBuilder.Entity("RoleUser", b =>
                {
                    b.Property<int>("RolesId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UsersId")
                        .HasColumnType("INTEGER");

                    b.HasKey("RolesId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("RoleUser");
                });

            modelBuilder.Entity("TeamTeam", b =>
                {
                    b.Property<int>("ChildTeamsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TeamId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ChildTeamsId", "TeamId");

                    b.HasIndex("TeamId");

                    b.ToTable("TeamTeam");
                });

            modelBuilder.Entity("TeamUser", b =>
                {
                    b.Property<int>("TeamsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UsersId")
                        .HasColumnType("INTEGER");

                    b.HasKey("TeamsId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("TeamUser");
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
                        .OnDelete(DeleteBehavior.Cascade)
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
                        .OnDelete(DeleteBehavior.Cascade)
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
                });
#pragma warning restore 612, 618
        }
    }
}
