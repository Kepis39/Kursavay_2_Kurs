using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryApp.Models;
using LibraryApp.Services;
using MsBox.Avalonia;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LibraryApp.ViewModels;

public partial class BooksViewModel : ViewModelBase
{
    private readonly DataService _data = new();
    private readonly ImportService _import = new();

    [ObservableProperty] private ObservableCollection<Book> _books = new();
    [ObservableProperty] private Book? _selectedBook;
    [ObservableProperty] private string _newBookId = "";
    [ObservableProperty] private string _newTitle = "";
    [ObservableProperty] private string _newAuthor = "";
    [ObservableProperty] private string _newPublisher = "";
    [ObservableProperty] private string _newPlace = "";
    [ObservableProperty] private string _newYear = "";
    [ObservableProperty] private string _newPages = "";
    [ObservableProperty] private string _newPrice = "";
    [ObservableProperty] private string _status = "";

    public BooksViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await Load();
    }

    [RelayCommand] private async Task Load()
    {
        Books = new ObservableCollection<Book>(await _data.GetBooksAsync());
        Status = $"Books: {Books.Count}";
    }

    [RelayCommand] private async Task Add()
    {
        if (string.IsNullOrWhiteSpace(NewBookId) || string.IsNullOrWhiteSpace(NewTitle)) { Status = "Enter BookId and Title"; return; }
        int? year = string.IsNullOrWhiteSpace(NewYear) ? null : (int.TryParse(NewYear, out var y) ? y : null);
        int? pages = string.IsNullOrWhiteSpace(NewPages) ? null : (int.TryParse(NewPages, out var p) ? p : null);
        double? price = string.IsNullOrWhiteSpace(NewPrice) ? null : (double.TryParse(NewPrice, out var pr) ? pr : null);
        var b = new Book { BookId = NewBookId.Trim(), Title = NewTitle.Trim(), FirstAuthor = NewAuthor, Publisher = NewPublisher, PlaceOfPublication = NewPlace, YearOfPublication = year, PageCount = pages, Price = price };
        if (await _data.AddBookAsync(b)) { Clear(); await Load(); Status = "Book added"; }
        else Status = "Error";
    }

    [RelayCommand] private async Task Update()
    {
        if (SelectedBook == null) return;
        int? year = string.IsNullOrWhiteSpace(NewYear) ? null : (int.TryParse(NewYear, out var y) ? y : null);
        int? pages = string.IsNullOrWhiteSpace(NewPages) ? null : (int.TryParse(NewPages, out var p) ? p : null);
        double? price = string.IsNullOrWhiteSpace(NewPrice) ? null : (double.TryParse(NewPrice, out var pr) ? pr : null);
        SelectedBook.Title = NewTitle; SelectedBook.FirstAuthor = NewAuthor; SelectedBook.Publisher = NewPublisher; SelectedBook.PlaceOfPublication = NewPlace; SelectedBook.YearOfPublication = year; SelectedBook.PageCount = pages; SelectedBook.Price = price;
        if (await _data.UpdateBookAsync(SelectedBook)) { await Load(); Status = "Updated"; }
        else Status = "Error";
    }

    [RelayCommand] private async Task Delete()
    {
        if (SelectedBook == null) return;
        var box = MessageBoxManager.GetMessageBoxStandard("Confirm", $"Delete {SelectedBook.Title}?", MsBox.Avalonia.Enums.ButtonEnum.YesNo);
        if (await box.ShowAsync() == MsBox.Avalonia.Enums.ButtonResult.Yes && await _data.DeleteBookAsync(SelectedBook.BookId))
        { await Load(); Status = "Deleted"; }
    }

    [RelayCommand] private async Task ImportCsv() => await PickAndImport("csv", _import.ImportBooksFromCsvAsync);
    [RelayCommand] private async Task ImportExcel() => await PickAndImport("xlsx", _import.ImportBooksFromExcelAsync);
    [RelayCommand] private async Task ImportJson() => await PickAndImport("json", _import.ImportBooksFromJsonAsync);

    partial void OnSelectedBookChanged(Book? value)
    {
        if (value != null)
        { 
            NewBookId = value.BookId; 
            NewTitle = value.Title;
            NewAuthor = value.FirstAuthor ?? "";
            NewPublisher = value.Publisher ?? "";
            NewPlace = value.PlaceOfPublication ?? "";
            NewYear = value.YearOfPublication?.ToString() ?? "";
            NewPages = value.PageCount?.ToString() ?? "";
            NewPrice = value.Price?.ToString() ?? ""; 
        }
    }

    private async Task PickAndImport(string ext, Func<string, Task<bool>> action)
    {
        var w = GetWindow(); if (w == null) return;
        var files = await w.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType(ext.ToUpper()) { Patterns = new[] { $"*.{ext}" } } } });
        if (files.Count > 0) { var path = files[0].TryGetLocalPath(); if (string.IsNullOrEmpty(path)) { Status = "No path"; return; } if (await action(path)) { await Load(); Status = "Import OK"; } else Status = $"Import failed: {string.Join(", ", _import.ImportErrors)}"; }
    }

    private static Avalonia.Controls.Window? GetWindow() => (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime)?.MainWindow;
    private void Clear() { NewBookId = NewTitle = NewAuthor = NewPublisher = NewPlace = NewYear = NewPages = NewPrice = ""; }
}
