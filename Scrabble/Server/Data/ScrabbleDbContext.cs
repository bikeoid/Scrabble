using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Scrabble.Server.Data;

public partial class ScrabbleDbContext : DbContext
{
    public ScrabbleDbContext()
    {
    }

    public ScrabbleDbContext(DbContextOptions<ScrabbleDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<Game> Games { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerGame> PlayerGames { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:ScrabbleDbConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.Property(e => e.ChatDate).HasColumnType("datetime");
            entity.Property(e => e.ChatText)
                .IsRequired()
                .HasColumnType("ntext");

            entity.HasOne(d => d.Game).WithMany(p => p.Chats)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Chats_Games");

            entity.HasOne(d => d.Player).WithMany(p => p.Chats)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Chats_Players");
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.Property(e => e.GameState)
                .IsRequired()
                .HasColumnType("ntext");
            entity.Property(e => e.LastMoveOn).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.WinnerName)
                .IsRequired()
                .HasMaxLength(80);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PlayerGame>(entity =>
        {
            entity.HasOne(d => d.Game).WithMany(p => p.PlayerGames)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PlayerGames_Games");

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerGames)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PlayerGames_Players");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
