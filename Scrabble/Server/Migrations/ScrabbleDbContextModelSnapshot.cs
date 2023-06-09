﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Scrabble.Server.Data;

#nullable disable

namespace Scrabble.Server.Migrations
{
    [DbContext(typeof(ScrabbleDbContext))]
    partial class ScrabbleDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Scrabble.Server.Data.Chat", b =>
                {
                    b.Property<int>("ChatId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ChatId"));

                    b.Property<DateTime>("ChatDate")
                        .HasColumnType("datetime");

                    b.Property<string>("ChatText")
                        .IsRequired()
                        .HasColumnType("ntext");

                    b.Property<int>("GameId")
                        .HasColumnType("int");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.HasKey("ChatId");

                    b.HasIndex("GameId");

                    b.HasIndex("PlayerId");

                    b.ToTable("Chats");
                });

            modelBuilder.Entity("Scrabble.Server.Data.Game", b =>
                {
                    b.Property<int>("GameId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("GameId"));

                    b.Property<bool>("Active")
                        .HasColumnType("bit");

                    b.Property<string>("GameState")
                        .IsRequired()
                        .HasColumnType("ntext");

                    b.Property<DateTime>("LastMoveOn")
                        .HasColumnType("datetime");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime");

                    b.Property<int>("WinType")
                        .HasColumnType("int");

                    b.Property<int>("WinnerId")
                        .HasColumnType("int");

                    b.Property<string>("WinnerName")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.HasKey("GameId");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("Scrabble.Server.Data.Player", b =>
                {
                    b.Property<int>("PlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("PlayerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(80)
                        .IsUnicode(false)
                        .HasColumnType("varchar(80)");

                    b.Property<bool>("EnableSound")
                        .HasColumnType("bit");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("bit");

                    b.Property<bool>("IsPlayer")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(80)
                        .IsUnicode(false)
                        .HasColumnType("varchar(80)");

                    b.Property<bool>("NotifyNewMoveByEmail")
                        .HasColumnType("bit");

                    b.Property<bool>("WordCheck")
                        .HasColumnType("bit");

                    b.HasKey("PlayerId");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("Scrabble.Server.Data.PlayerGame", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("GameId")
                        .HasColumnType("int");

                    b.Property<int>("HighestChatSeen")
                        .HasColumnType("int");

                    b.Property<bool>("MyMove")
                        .HasColumnType("bit");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<int>("PlayerScore")
                        .HasColumnType("int");

                    b.Property<int>("WinType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("GameId");

                    b.HasIndex("PlayerId");

                    b.ToTable("PlayerGames");
                });

            modelBuilder.Entity("Scrabble.Server.Data.Chat", b =>
                {
                    b.HasOne("Scrabble.Server.Data.Game", "Game")
                        .WithMany("Chats")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Chats_Games");

                    b.HasOne("Scrabble.Server.Data.Player", "Player")
                        .WithMany("Chats")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Chats_Players");

                    b.Navigation("Game");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("Scrabble.Server.Data.PlayerGame", b =>
                {
                    b.HasOne("Scrabble.Server.Data.Game", "Game")
                        .WithMany("PlayerGames")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_PlayerGames_Games");

                    b.HasOne("Scrabble.Server.Data.Player", "Player")
                        .WithMany("PlayerGames")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_PlayerGames_Players");

                    b.Navigation("Game");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("Scrabble.Server.Data.Game", b =>
                {
                    b.Navigation("Chats");

                    b.Navigation("PlayerGames");
                });

            modelBuilder.Entity("Scrabble.Server.Data.Player", b =>
                {
                    b.Navigation("Chats");

                    b.Navigation("PlayerGames");
                });
#pragma warning restore 612, 618
        }
    }
}
