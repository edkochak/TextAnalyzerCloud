﻿// <auto-generated />
using System;
using FileAnalysisService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FileAnalysisService.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250517121550_InitialCreateAnalysis")]
    partial class InitialCreateAnalysis
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Shared.Models.AnalysisResult", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("AnalyzedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("CharacterCount")
                        .HasColumnType("integer");

                    b.Property<Guid>("FileId")
                        .HasColumnType("uuid");

                    b.Property<int>("ParagraphCount")
                        .HasColumnType("integer");

                    b.Property<string>("WordCloudLocation")
                        .HasColumnType("text");

                    b.Property<int>("WordCount")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("AnalysisResults");
                });
#pragma warning restore 612, 618
        }
    }
}
