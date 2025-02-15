﻿// <auto-generated />
using System;
using ImgApiForNg.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ImgApiForNg.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250215173652_PropAdd")]
    partial class PropAdd
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.20")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ImgApiForNg.Models.Employee", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("birthdate")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("company")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("education")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("filebytes")
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("filename")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("filetype")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("firstname")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("gender")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("jobExperience")
                        .HasColumnType("int");

                    b.Property<string>("lastname")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("salary")
                        .HasColumnType("int");

                    b.HasKey("id");

                    b.ToTable("Employees");
                });

            modelBuilder.Entity("ImgApiForNg.Models.Item", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("DownloadToken")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("DownloadTokenExpiration")
                        .HasColumnType("datetime2");

                    b.Property<string>("fileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fileSize")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fileType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fileUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("ImgApiForNg.Models.Man", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("base64string")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("filename")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("filesize")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("filetype")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("imagebytes")
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("Men");
                });

            modelBuilder.Entity("ImgApiForNg.Models.Person", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("apiurl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("city")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("filebytes")
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("filename")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("filesize")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("filetype")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("Persons");
                });

            modelBuilder.Entity("ImgApiForNg.Models.Prop", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("DownloadToken")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("DownloadTokenExpiration")
                        .HasColumnType("datetime2");

                    b.Property<string>("fileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fileSize")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fileType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fileUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("Props");
                });
#pragma warning restore 612, 618
        }
    }
}
