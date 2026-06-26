using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryApp.Models;
using LibraryApp.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryApp.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    private readonly DataService _data = new();
    private readonly ExportService _export = new();

    [ObservableProperty] private ObservableCollection<BookReportItem> _bookReportItems = new();
    [ObservableProperty] private ObservableCollection<ReaderReportItem> _readerReportItems = new();
    [ObservableProperty] private ObservableCollection<ThemeReportItem> _themeReportItems = new();
    [ObservableProperty] private ObservableCollection<Book> _unloanedBooks = new();
    [ObservableProperty] private ObservableCollection<Reader> _debtors = new();
    [ObservableProperty] private int _currentReport;
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isBookReportVisible;
    [ObservableProperty] private bool _isReaderReportVisible;
    [ObservableProperty] private bool _isThemeReportVisible;
    [ObservableProperty] private bool _isUnloanedVisible;
    [ObservableProperty] private bool _isDebtorsVisible;

    private void ResetVisibility()
    {
        IsBookReportVisible = IsReaderReportVisible = IsThemeReportVisible = IsUnloanedVisible = IsDebtorsVisible = false;
    }

    [RelayCommand] private async Task ShowBookReport()
    {
        ResetVisibility(); CurrentReport = 0; IsBookReportVisible = true;
        await using var ctx = new LibraryabonementContext();
        var books = await ctx.Books.AsNoTracking().ToListAsync();
        var copies = await ctx.Copies.AsNoTracking().ToListAsync();
        var loans = await ctx.Issues.AsNoTracking().ToListAsync();

        BookReportItems = new ObservableCollection<BookReportItem>(books.Select(b => new BookReportItem
        {
            BookId = b.BookId,
            Title = b.Title,
            Author = b.FirstAuthor ?? "",
            Publisher = b.Publisher ?? "",
            Year = b.YearOfPublication ?? 0,
            TotalCopies = copies.Count(c => c.BookId == b.BookId),
            LoanedCopies = loans.Count(l => l.InventoryNumber != null && copies.Any(c => c.InventoryNumber == l.InventoryNumber && c.BookId == b.BookId && l.ReturnDate == null)),
            AvailableCopies = copies.Count(c => c.BookId == b.BookId) - loans.Count(l => l.InventoryNumber != null && copies.Any(c => c.InventoryNumber == l.InventoryNumber && c.BookId == b.BookId && l.ReturnDate == null))
        }));
        Status = $"Books: {BookReportItems.Count}";
    }

    [RelayCommand] private async Task ShowReaderReport()
    {
        ResetVisibility(); CurrentReport = 1; IsReaderReportVisible = true;
        await using var ctx = new LibraryabonementContext();
        var readers = await ctx.Readers.AsNoTracking().ToListAsync();
        var loans = await ctx.Issues.AsNoTracking().ToListAsync();

        ReaderReportItems = new ObservableCollection<ReaderReportItem>(readers.Select(r => new ReaderReportItem
        {
            FullName = r.FullName,
            BirthDate = r.BirthDate?.ToString("yyyy-MM-dd") ?? "",
            Phone = r.Phone ?? "",
            TotalBooks = loans.Count(l => l.TicketNumber == r.TicketNumber),
            ReturnedBooks = loans.Count(l => l.TicketNumber == r.TicketNumber && l.ReturnDate != null),
            DebtCount = loans.Count(l => l.TicketNumber == r.TicketNumber && l.ReturnDate == null && l.IssueDate != null && (DateTime.Now - l.IssueDate.Value).TotalDays > 14)
        }));
        Status = $"Readers: {ReaderReportItems.Count}";
    }

    [RelayCommand] private async Task ShowThemeReport()
    {
        ResetVisibility(); CurrentReport = 2; IsThemeReportVisible = true;
        await using var ctx = new LibraryabonementContext();
        var themes = await ctx.ThematicCatalogs.AsNoTracking().ToListAsync();
        var bookCats = await ctx.BookCatalogs.AsNoTracking().ToListAsync();
        var copies = await ctx.Copies.AsNoTracking().ToListAsync();
        var loans = await ctx.Issues.AsNoTracking().ToListAsync();

        ThemeReportItems = new ObservableCollection<ThemeReportItem>(themes.Select(t => new ThemeReportItem
        {
            ThemeName = t.ThemeName,
            BookCount = bookCats.Count(bc => bc.ThemeCode == t.ThemeCode),
            CopyCount = copies.Count(c => c.ThemeCode == t.ThemeCode),
            LoanCount = loans.Count(l => copies.Any(c => c.InventoryNumber == l.InventoryNumber && c.ThemeCode == t.ThemeCode))
        }));
        Status = $"Themes: {ThemeReportItems.Count}";
    }

    [RelayCommand] private async Task ShowUnloaned()
    {
        ResetVisibility(); CurrentReport = 3; IsUnloanedVisible = true;
        await using var ctx = new LibraryabonementContext();
        var allBooks = await ctx.Books.AsNoTracking().ToListAsync();
        var loanedInvNumbers = await ctx.Issues.AsNoTracking().Where(l => l.ReturnDate == null).Select(l => l.InventoryNumber).ToListAsync();
        var copies = await ctx.Copies.AsNoTracking().ToListAsync();
        var loanedBooks = copies.Where(c => c.InventoryNumber != null && loanedInvNumbers.Contains(c.InventoryNumber)).Select(c => c.BookId).Distinct().ToList();

        UnloanedBooks = new ObservableCollection<Book>(allBooks.Where(b => !loanedBooks.Contains(b.BookId)));
        Status = $"Unloaned: {UnloanedBooks.Count}";
    }

    [RelayCommand] private async Task ShowDebtors()
    {
        ResetVisibility(); CurrentReport = 4; IsDebtorsVisible = true;
        await using var ctx = new LibraryabonementContext();
        var today = DateTime.Now;
        var overdueLoans = await ctx.Issues
            .AsNoTracking()
            .Where(l => l.ReturnDate == null && l.IssueDate != null && (today - l.IssueDate.Value).TotalDays > 14)
            .Select(l => l.TicketNumber)
            .Distinct()
            .ToListAsync();

        var allReaders = await ctx.Readers.AsNoTracking().ToListAsync();
        Debtors = new ObservableCollection<Reader>(allReaders.Where(r => overdueLoans.Contains(r.TicketNumber)));
        Status = $"Debtors: {Debtors.Count}";
    }

    [RelayCommand] private async Task ExportExcel()
    {
        var w = GetWindow(); if (w == null) return;
        var file = await w.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            DefaultExtension = "xlsx",
            FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } } }
        });
        if (file == null) return;
        bool r = false;
        switch (CurrentReport)
        {
            case 0:
                r = await _export.ExportReportToExcelAsync(file.TryGetLocalPath()!, "Books",
                    BookReportItems.Select(x => new[] { x.BookId, x.Title, x.Author, x.Publisher, x.Year.ToString(), x.TotalCopies.ToString(), x.LoanedCopies.ToString(), x.AvailableCopies.ToString() }).ToList(),
                    new[] { "BookId", "Title", "Author", "Publisher", "Year", "Total", "Loaned", "Available" });
                break;
        }
        Status = r ? "Export completed" : "Error";
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        var w = GetWindow(); if (w == null) return;
        var file = await w.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            DefaultExtension = "pdf",
            FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } }
        });
        if (file == null) return;
        bool r = false;
        switch (CurrentReport)
        {
            case 0:
                r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Book Report",
                    BookReportItems.Select(x => new[] { x.BookId, x.Title, x.Author, x.Publisher, x.Year.ToString(), x.TotalCopies.ToString(), x.LoanedCopies.ToString(), x.AvailableCopies.ToString() }).ToList(),
                    new[] { "BookId", "Title", "Author", "Publisher", "Year", "Total", "Loaned", "Available" });
                break;
            case 1:
                r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Reader Report",
                    ReaderReportItems.Select(x => new[] { x.FullName, x.BirthDate, x.Phone, x.TotalBooks.ToString(), x.ReturnedBooks.ToString(), x.DebtCount.ToString() }).ToList(),
                    new[] { "FullName", "BirthDate", "Phone", "Total", "Returned", "Debts" });
                break;
            case 2:
                r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Theme Report",
                    ThemeReportItems.Select(x => new[] { x.ThemeName, x.BookCount.ToString(), x.CopyCount.ToString(), x.LoanCount.ToString() }).ToList(),
                    new[] { "Theme", "Books", "Copies", "Loans" });
                break;
            case 3:
                r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Unloaned Books",
                    UnloanedBooks.Select(x => new[] { x.BookId, x.Title, x.FirstAuthor ?? "", x.YearOfPublication?.ToString() ?? "" }).ToList(),
                    new[] { "BookId", "Title", "Author", "Year" });
                break;
            case 4:
                r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Debtors",
                    Debtors.Select(x => new[] { x.TicketNumber.ToString(), x.FullName, x.BirthDate?.ToString("yyyy-MM-dd") ?? "", x.Phone ?? "" }).ToList(),
                    new[] { "Ticket", "FullName", "BirthDate", "Phone" });
                break;
        }
        Status = r ? "PDF Export completed" : "PDF Export failed";
    }



    private static Avalonia.Controls.Window? GetWindow() =>
        (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime)?.MainWindow;
}

public class BookReportItem
{
    public string BookId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Publisher { get; set; } = "";
    public int Year { get; set; }
    public int TotalCopies { get; set; }
    public int LoanedCopies { get; set; }
    public int AvailableCopies { get; set; }
}

public class ReaderReportItem
{
    public string FullName { get; set; } = "";
    public string BirthDate { get; set; } = "";
    public string Phone { get; set; } = "";
    public int TotalBooks { get; set; }
    public int ReturnedBooks { get; set; }
    public int DebtCount { get; set; }
}

public class ThemeReportItem
{
    public string ThemeName { get; set; } = "";
    public int BookCount { get; set; }
    public int CopyCount { get; set; }
    public int LoanCount { get; set; }
}
