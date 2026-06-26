using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryApp.Models;
using LibraryApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryApp.ViewModels;

public partial class ImportExportViewModel : ViewModelBase
{
    private readonly DataService _data = new();
    private readonly ImportService _import = new();
    private readonly ExportService _export = new();

    [ObservableProperty] private string _status = "Select operation";



    [ObservableProperty] private ObservableCollection<BookReportItem> _bookReportItems = new();
    [ObservableProperty] private ObservableCollection<ReaderReportItem> _readerReportItems = new();
    [ObservableProperty] private ObservableCollection<ThemeReportItem> _themeReportItems = new();

    [ObservableProperty] private ObservableCollection<Book> _books = new();
    [ObservableProperty] private ObservableCollection<Copy> _copies = new();
    [ObservableProperty] private ObservableCollection<Reader> _readers = new();
    [ObservableProperty] private ObservableCollection<ThematicCatalog> _themes = new();
    [ObservableProperty] private ObservableCollection<BookCatalog> _bookCatalogs = new();
    [ObservableProperty] private ObservableCollection<DistributionToReader> _issues = new();

    public ImportExportViewModel()
    {
        _ = LoadAllAsync();
    }

    [RelayCommand] private async Task LoadAllAsync()
    {
        Books = new ObservableCollection<Book>(await _data.GetBooksAsync());
        Copies = new ObservableCollection<Copy>(await _data.GetCopiesAsync());
        Readers = new ObservableCollection<Reader>(await _data.GetReadersAsync());
        Themes = new ObservableCollection<ThematicCatalog>(await _data.GetThemesAsync());
        await using var ctx = new LibraryabonementContext();
        BookCatalogs = new ObservableCollection<BookCatalog>(await ctx.BookCatalogs.AsNoTracking().ToListAsync());
        Issues = new ObservableCollection<DistributionToReader>(await ctx.Issues.AsNoTracking().ToListAsync());
        Status = $"Loaded: {Books.Count} books, {Copies.Count} copies, {Readers.Count} readers, {Themes.Count} themes, {BookCatalogs.Count} book-catalogs, {Issues.Count} issues";
    }

    [RelayCommand] private async Task ImportJsonAll()
    {
        var w = GetWindow(); if (w == null) return;
        var files = await w.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } }
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath(); if (string.IsNullOrEmpty(path)) { Status = "No path"; return; }
        if (await _import.ImportFromJsonAsync(path)) { await LoadAllAsync(); Status = "Full JSON import OK"; }
        else Status = $"Import failed: {string.Join(", ", _import.ImportErrors)}";
    }

    [RelayCommand] private async Task ImportXmlAll()
    {
        var w = GetWindow(); if (w == null) return;
        var files = await w.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("XML") { Patterns = new[] { "*.xml" } } }
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath(); if (string.IsNullOrEmpty(path)) { Status = "No path"; return; }
        if (await _import.ImportFromXmlAsync(path)) { await LoadAllAsync(); Status = "Full XML import OK"; }
        else Status = $"Import failed: {string.Join(", ", _import.ImportErrors)}";
    }

    [RelayCommand] private async Task ExportJsonAll()
    {
        var w = GetWindow(); if (w == null) return;
        var file = await w.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "json",
            FileTypeChoices = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } }
        });
        if (file == null) return;
        var path = file.TryGetLocalPath(); if (string.IsNullOrEmpty(path)) { Status = "No path"; return; }
        bool r = await _export.ExportAllToJsonAsync(path, Books.ToList(), Readers.ToList(), Themes.ToList(), Copies.ToList(), BookCatalogs.ToList(), Issues.ToList());
        Status = r ? "Export JSON OK" : "Export failed";
    }
    //[RelayCommand]
    //private async Task ExportPdf()
    //{
    //    var w = GetWindow(); if (w == null) return;
    //    var file = await w.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
    //    {
    //        DefaultExtension = "pdf",
    //        FileTypeChoices = new[] { new Avalonia.Platform.Storage.FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } }
    //    });
    //    if (file == null) return;
    //    bool r = false;
    //    switch (CurrentReport)
    //    {
    //        case 0:
    //            r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Book Report",
    //                BookReportItems.Select(x => new[] { x.BookId, x.Title, x.Author, x.Publisher, x.Year.ToString(), x.TotalCopies.ToString(), x.LoanedCopies.ToString(), x.AvailableCopies.ToString() }).ToList(),
    //                new[] { "BookId", "Title", "Author", "Publisher", "Year", "Total", "Loaned", "Available" });
    //            break;
    //        case 1:
    //            r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Reader Report",
    //                ReaderReportItems.Select(x => new[] { x.FullName, x.BirthDate, x.Phone, x.TotalBooks.ToString(), x.ReturnedBooks.ToString(), x.DebtCount.ToString() }).ToList(),
    //                new[] { "FullName", "BirthDate", "Phone", "Total", "Returned", "Debts" });
    //            break;
    //        case 2:
    //            r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Theme Report",
    //                ThemeReportItems.Select(x => new[] { x.ThemeName, x.BookCount.ToString(), x.CopyCount.ToString(), x.LoanCount.ToString() }).ToList(),
    //                new[] { "Theme", "Books", "Copies", "Loans" });
    //            break;
    //        case 3:
    //            r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Unloaned Books",
    //                UnloanedBooks.Select(x => new[] { x.BookId, x.Title, x.FirstAuthor ?? "", x.YearOfPublication?.ToString() ?? "" }).ToList(),
    //                new[] { "BookId", "Title", "Author", "Year" });
    //            break;
    //        case 4:
    //            r = await _export.ExportReportToPdfAsync(file.TryGetLocalPath() ?? "", "Debtors",
    //                Debtors.Select(x => new[] { x.TicketNumber.ToString(), x.FullName, x.BirthDate?.ToString("yyyy-MM-dd") ?? "", x.Phone ?? "" }).ToList(),
    //                new[] { "Ticket", "FullName", "BirthDate", "Phone" });
    //            break;
    //    }
    //    Status = r ? "PDF Export completed" : "PDF Export failed";
    //}




    private static Avalonia.Controls.Window? GetWindow() =>
        (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime)?.MainWindow;
}




