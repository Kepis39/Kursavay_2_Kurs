using System;
using System.Collections.Generic;
using LibraryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp;

public partial class LibraryabonementContext : DbContext
{
    public LibraryabonementContext()
    {
    Database.EnsureCreated();
    }

    public LibraryabonementContext(DbContextOptions<LibraryabonementContext> options)
        : base(options)
    {
    Database.EnsureCreated();
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<BookCatalog> BookCatalogs { get; set; }

    public virtual DbSet<Copy> Copies { get; set; }

    public virtual DbSet<DistributionToReader> Issues { get; set; }

    public virtual DbSet<Reader> Readers { get; set; }

    public virtual DbSet<ThematicCatalog> ThematicCatalogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("Data Source=C:\\kURS\\libraryabonement.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.FirstAuthor).HasColumnName("first_author");
            entity.Property(e => e.PageCount).HasColumnName("page_count");
            entity.Property(e => e.PlaceOfPublication).HasColumnName("place_of_publication");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Publisher).HasColumnName("publisher");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.YearOfPublication).HasColumnName("year_of_publication");
        });

        modelBuilder.Entity<BookCatalog>(entity =>
        {
            entity.HasKey(e => e.CatalogId);

            entity.ToTable("Book_Catalog");

            entity.HasIndex(e => new { e.BookId, e.ThemeCode }, "IX_Book_Catalog_book_id_theme_code").IsUnique();

            entity.HasIndex(e => e.BookId, "idx_book_catalog_book_id");

            entity.HasIndex(e => e.ThemeCode, "idx_book_catalog_theme_code");

            entity.Property(e => e.CatalogId).HasColumnName("catalog_id");
            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.ThemeCode).HasColumnName("theme_code");

            entity.HasOne(d => d.Book).WithMany(p => p.BookCatalogs).HasForeignKey(d => d.BookId);

            entity.HasOne(d => d.ThemeCodeNavigation).WithMany(p => p.BookCatalogs).HasForeignKey(d => d.ThemeCode);
        });

        modelBuilder.Entity<Copy>(entity =>
        {
            entity.HasKey(e => e.InventoryNumber);

            entity.HasIndex(e => e.BookId, "idx_copies_book_id");

            entity.HasIndex(e => e.ThemeCode, "idx_copies_theme_code");

            entity.Property(e => e.InventoryNumber).HasColumnName("inventory_number");
            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.CopyCount).HasColumnName("copy_count");
            entity.Property(e => e.ThemeCode).HasColumnName("theme_code");

            entity.HasOne(d => d.Book).WithMany(p => p.Copies).HasForeignKey(d => d.BookId);

            entity.HasOne(d => d.ThemeCodeNavigation).WithMany(p => p.Copies)
                .HasForeignKey(d => d.ThemeCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DistributionToReader>(entity =>
        {
            entity.ToTable("Issue");
            entity.HasKey(e => e.IssueId);

            entity.HasIndex(e => e.InventoryNumber, "idx_issue_inventory");

            entity.HasIndex(e => e.IssueDate, "idx_issue_issue_date");

            entity.HasIndex(e => e.TicketNumber, "idx_issue_ticket");

            entity.Property(e => e.IssueId).HasColumnName("issue_id");
            entity.Property(e => e.ActualReturn)
                .HasColumnType("DATE")
                .HasColumnName("actual_return");
            entity.Property(e => e.InventoryNumber).HasColumnName("inventory_number");
            entity.Property(e => e.IssueDate)
                .HasColumnType("DATE")
                .HasColumnName("issue_date");
            entity.Property(e => e.ReturnDate)
                .HasColumnType("DATE")
                .HasColumnName("return_date");
            entity.Property(e => e.TicketNumber).HasColumnName("ticket_number");

            entity.HasOne(d => d.InventoryNumberNavigation).WithMany(p => p.Issues)
                .HasForeignKey(d => d.InventoryNumber)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.TicketNumberNavigation).WithMany(p => p.Issues)
                .HasForeignKey(d => d.TicketNumber)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reader>(entity =>
        {
            entity.HasKey(e => e.TicketNumber);

            entity.Property(e => e.TicketNumber)
                .ValueGeneratedNever()
                .HasColumnName("ticket_number");
            entity.Property(e => e.BirthDate)
                .HasColumnType("DATE")
                .HasColumnName("birth_date");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.Phone).HasColumnName("phone");
        });

        modelBuilder.Entity<ThematicCatalog>(entity =>
        {
            entity.HasKey(e => e.ThemeCode);

            entity.ToTable("Thematic_Catalogs");

            entity.HasIndex(e => e.ThemeName, "IX_Thematic_Catalogs_theme_name").IsUnique();

            entity.Property(e => e.ThemeCode)
                .ValueGeneratedNever()
                .HasColumnName("theme_code");
            entity.Property(e => e.ThemeName).HasColumnName("theme_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
