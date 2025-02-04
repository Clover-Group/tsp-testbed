﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TspTestbed.Data;

#nullable disable

namespace TspTestbed.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250124065650_InitialBasicInfo")]
    partial class InitialBasicInfo
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "hstore");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("TspTestbed.Models.Sink", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("JdbcString")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("jdbc_string");

                    b.HasKey("Id")
                        .HasName("pk_sinks");

                    b.ToTable("sinks", (string)null);
                });

            modelBuilder.Entity("TspTestbed.Models.Source", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("JdbcString")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("jdbc_string");

                    b.HasKey("Id")
                        .HasName("pk_sources");

                    b.ToTable("sources", (string)null);
                });

            modelBuilder.Entity("TspTestbed.Models.Test", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("SinkId")
                        .HasColumnType("uuid")
                        .HasColumnName("sink_id");

                    b.Property<Guid>("SourceId")
                        .HasColumnType("uuid")
                        .HasColumnName("source_id");

                    b.HasKey("Id")
                        .HasName("pk_tests");

                    b.HasIndex("SinkId")
                        .HasDatabaseName("ix_tests_sink_id");

                    b.HasIndex("SourceId")
                        .HasDatabaseName("ix_tests_source_id");

                    b.ToTable("tests", (string)null);
                });

            modelBuilder.Entity("TspTestbed.Models.TestRun", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<TimeSpan>("RunningTime")
                        .HasColumnType("interval")
                        .HasColumnName("running_time");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<Guid>("TestId")
                        .HasColumnType("uuid")
                        .HasColumnName("test_id");

                    b.HasKey("Id")
                        .HasName("pk_test_runs");

                    b.HasIndex("TestId")
                        .HasDatabaseName("ix_test_runs_test_id");

                    b.ToTable("test_runs", (string)null);
                });

            modelBuilder.Entity("TspTestbed.Models.Test", b =>
                {
                    b.HasOne("TspTestbed.Models.Sink", "Sink")
                        .WithMany()
                        .HasForeignKey("SinkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_tests_sinks_sink_id");

                    b.HasOne("TspTestbed.Models.Source", "Source")
                        .WithMany()
                        .HasForeignKey("SourceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_tests_sources_source_id");

                    b.OwnsMany("TspTestbed.Models.Incident", "Incidents", b1 =>
                        {
                            b1.Property<Guid>("TestId")
                                .HasColumnType("uuid");

                            b1.Property<int>("__synthesizedOrdinal")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer");

                            b1.Property<DateTime>("From")
                                .HasColumnType("timestamp with time zone");

                            b1.Property<int>("PatternId")
                                .HasColumnType("integer");

                            b1.Property<int?>("Subunit")
                                .HasColumnType("integer");

                            b1.Property<DateTime>("To")
                                .HasColumnType("timestamp with time zone");

                            b1.HasKey("TestId", "__synthesizedOrdinal");

                            b1.ToTable("tests");

                            b1.ToJson("incidents");

                            b1.WithOwner()
                                .HasForeignKey("TestId")
                                .HasConstraintName("fk_tests_tests_test_id");
                        });

                    b.OwnsMany("TspTestbed.Models.Pattern", "Patterns", b1 =>
                        {
                            b1.Property<Guid>("TestId")
                                .HasColumnType("uuid");

                            b1.Property<int>("__synthesizedOrdinal")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer");

                            b1.Property<int>("Id")
                                .HasColumnType("integer");

                            b1.Property<Dictionary<string, string>>("Metadata")
                                .HasColumnType("hstore");

                            b1.Property<string>("SourceCode")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.Property<int?>("Subunit")
                                .HasColumnType("integer");

                            b1.HasKey("TestId", "__synthesizedOrdinal");

                            b1.ToTable("tests");

                            b1.ToJson("patterns");

                            b1.WithOwner()
                                .HasForeignKey("TestId")
                                .HasConstraintName("fk_tests_tests_test_id");
                        });

                    b.Navigation("Incidents");

                    b.Navigation("Patterns");

                    b.Navigation("Sink");

                    b.Navigation("Source");
                });

            modelBuilder.Entity("TspTestbed.Models.TestRun", b =>
                {
                    b.HasOne("TspTestbed.Models.Test", "Test")
                        .WithMany("Runs")
                        .HasForeignKey("TestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_test_runs_tests_test_id");

                    b.Navigation("Test");
                });

            modelBuilder.Entity("TspTestbed.Models.Test", b =>
                {
                    b.Navigation("Runs");
                });
#pragma warning restore 612, 618
        }
    }
}
