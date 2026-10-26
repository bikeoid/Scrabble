using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Scrabble.Server.Data;

/// <summary>
/// SQLite version of ScrabbleDbContext for game data.
/// Use this context for SQLite database migrations and configuration.
/// </summary>
public partial class ScrabbleDbContextSQLite : DbContext
{
    public ScrabbleDbContextSQLite()
    {
    }

    public ScrabbleDbContextSQLite(DbContextOptions<ScrabbleDbContextSQLite> options)
        : base(options)
    {
    }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<Game> Games { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerGame> PlayerGames { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("Name=ConnectionStrings:ScrabbleDbSQLiteConnection");


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.Property(e => e.ChatDate).HasColumnType("TEXT");
            entity.Property(e => e.ChatText)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.HasOne(d => d.Game).WithMany(p => p.Chats)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Player).WithMany(p => p.Chats)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.Property(e => e.GameState)
                .IsRequired()
                .HasColumnType("TEXT");
            entity.Property(e => e.LastMoveOn).HasColumnType("TEXT");
            entity.Property(e => e.StartDate).HasColumnType("TEXT");
            entity.Property(e => e.WinnerName)
                .IsRequired()
                .HasMaxLength(80);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(80);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(80);
        });

        modelBuilder.Entity<PlayerGame>(entity =>
        {
            entity.HasOne(d => d.Game).WithMany(p => p.PlayerGames)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerGames)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
