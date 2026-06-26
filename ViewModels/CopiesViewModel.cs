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

public partial class CopiesViewModel : ViewModelBase
{
    private readonly DataService _data = new();
    private readonly ImportService _import = new();

    [ObservableProperty] private ObservableCollection<Copy> _copies = new();
    [ObservableProperty] private Copy? _selectedCopy;
    [ObservableProperty] private string _newInventoryNumber = "";
    [ObservableProperty] private string _newBookId = "";
    [ObservableProperty] private string _newThemeCode = "";
    [ObservableProperty] private string _newCopyCount = "";
    [ObservableProperty] private string _status = "";

    public CopiesViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await Load();
    }

    [RelayCommand] private async Task Load()
    {
        Copies = new ObservableCollection<Copy>(await _data.GetCopiesAsync());
        Status = $"Copies: {Copies.Count}";
    }

    [RelayCommand] private async Task Add()
    {
        if (string.IsNullOrWhiteSpace(NewInventoryNumber)) { Status = "Enter Inv#"; return; }
        int? theme = string.IsNullOrWhiteSpace(NewThemeCode) ? null : (int.TryParse(NewThemeCode, out var t) ? t : null);
        int? count = string.IsNullOrWhiteSpace(NewCopyCount) ? null : (int.TryParse(NewCopyCount, out var c) ? c : null);
        var copy = new Copy 
        { 
            InventoryNumber = NewInventoryNumber.Trim(), 
            BookId = NewBookId, 
            ThemeCode = theme,
            CopyCount = count
        };
        if (await _data.AddCopyAsync(copy)) { Clear(); await Load(); Status = "Copy added"; }
        else Status = "Error";
    }

    [RelayCommand] private async Task Update()
    {
        if (SelectedCopy == null) return;
        int? theme = string.IsNullOrWhiteSpace(NewThemeCode) ? null : (int.TryParse(NewThemeCode, out var t) ? t : null);
        int? count = string.IsNullOrWhiteSpace(NewCopyCount) ? null : (int.TryParse(NewCopyCount, out var c) ? c : null);
        SelectedCopy.BookId = NewBookId; 
        SelectedCopy.ThemeCode = theme;
        SelectedCopy.CopyCount = count;
        if (await _data.UpdateCopyAsync(SelectedCopy)) { await Load(); Status = "Updated"; }
        else Status = "Error";
    }

    [RelayCommand] private async Task Delete()
    {
        if (SelectedCopy == null) return;
        var box = MessageBoxManager.GetMessageBoxStandard("Confirm", $"Delete {SelectedCopy.InventoryNumber}?", MsBox.Avalonia.Enums.ButtonEnum.YesNo);
        if (await box.ShowAsync() == MsBox.Avalonia.Enums.ButtonResult.Yes && await _data.DeleteCopyAsync(SelectedCopy.InventoryNumber))
        { await Load(); Status = "Deleted"; }
    }

    [RelayCommand] private async Task ImportCsv() => await PickAndImport("csv", _import.ImportCopiesFromCsvAsync);
    [RelayCommand] private async Task ImportJson() => await PickAndImport("json", _import.ImportCopiesFromJsonAsync);

    partial void OnSelectedCopyChanged(Copy? value)
    {
        if (value != null)
        { 
            NewInventoryNumber = value.InventoryNumber; 
            NewBookId = value.BookId ?? ""; 
            NewThemeCode = value.ThemeCode?.ToString() ?? ""; 
            NewCopyCount = value.CopyCount?.ToString() ?? "";
        }
    }

    private async Task PickAndImport(string ext, Func<string, Task<bool>> action)
    {
        var w = GetWindow(); if (w == null) return;
        var files = await w.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType(ext.ToUpper()) { Patterns = new[] { $"*.{ext}" } } } });
        if (files.Count > 0) { var path = files[0].TryGetLocalPath(); if (string.IsNullOrEmpty(path)) { Status = "No path"; return; } if (await action(path)) { await Load(); Status = "Import OK"; } else Status = $"Import failed: {string.Join(", ", _import.ImportErrors)}"; }
    }

    private static Avalonia.Controls.Window? GetWindow() => (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime)?.MainWindow;
    private void Clear() { NewInventoryNumber = NewBookId = NewThemeCode = NewCopyCount = ""; }
}
