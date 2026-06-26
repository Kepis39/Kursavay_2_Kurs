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

public partial class ReadersViewModel : ViewModelBase
{
    private readonly DataService _data = new();
    private readonly ImportService _import = new();

    [ObservableProperty] private ObservableCollection<Reader> _readers = new();
    [ObservableProperty] private Reader? _selectedReader;
    [ObservableProperty] private string _newFullName = "";
    [ObservableProperty] private string _newBirthDate = "";
    [ObservableProperty] private string _newPhone = "";
    [ObservableProperty] private string _status = "";

    public ReadersViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await Load();
    }

    [RelayCommand] private async Task Load()
    {
        Readers = new ObservableCollection<Reader>(await _data.GetReadersAsync());
        Status = $"Readers: {Readers.Count}";
    }

    [RelayCommand] private async Task Add()
    {
        if (string.IsNullOrWhiteSpace(NewFullName)) { Status = "Enter FullName"; return; }
        var r = new Reader { FullName = NewFullName.Trim(), BirthDate = string.IsNullOrWhiteSpace(NewBirthDate) ? null : DateTime.Parse(NewBirthDate), Phone = string.IsNullOrWhiteSpace(NewPhone) ? null : NewPhone };
        if (await _data.AddReaderAsync(r)) { Clear(); await Load(); Status = "Reader added"; }
        else Status = "Error";
    }

    [RelayCommand] private async Task Update()
    {
        if (SelectedReader == null) return;
        SelectedReader.FullName = NewFullName; SelectedReader.BirthDate = string.IsNullOrWhiteSpace(NewBirthDate) ? null : DateTime.Parse(NewBirthDate); SelectedReader.Phone = string.IsNullOrWhiteSpace(NewPhone) ? null : NewPhone;
        if (await _data.UpdateReaderAsync(SelectedReader)) { await Load(); Status = "Updated"; }
        else Status = "Error";
    }

    [RelayCommand] private async Task Delete()
    {
        if (SelectedReader == null) return;
        var box = MessageBoxManager.GetMessageBoxStandard("Confirm", $"Delete {SelectedReader.FullName}?", MsBox.Avalonia.Enums.ButtonEnum.YesNo);
        if (await box.ShowAsync() == MsBox.Avalonia.Enums.ButtonResult.Yes && await _data.DeleteReaderAsync(SelectedReader.TicketNumber))
        { await Load(); Status = "Deleted"; }
    }

    [RelayCommand] private async Task ImportCsv() => await PickAndImport("csv", _import.ImportReadersFromCsvAsync);
    [RelayCommand] private async Task ImportExcel() => await PickAndImport("xlsx", _import.ImportReadersFromExcelAsync);
    [RelayCommand] private async Task ImportJson() => await PickAndImport("json", _import.ImportReadersFromJsonAsync);

    partial void OnSelectedReaderChanged(Reader? value)
    {
        if (value != null) { NewFullName = value.FullName; NewBirthDate = value.BirthDate?.ToString("yyyy-MM-dd") ?? ""; NewPhone = value.Phone ?? ""; }
    }

    private async Task PickAndImport(string ext, Func<string, Task<bool>> action)
    {
        var w = GetWindow(); if (w == null) return;
        var files = await w.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType(ext.ToUpper()) { Patterns = new[] { $"*.{ext}" } } } });
        if (files.Count > 0) { var path = files[0].TryGetLocalPath(); if (string.IsNullOrEmpty(path)) { Status = "No path"; return; } if (await action(path)) { await Load(); Status = "Import OK"; } else Status = $"Import failed: {string.Join(", ", _import.ImportErrors)}"; }
    }

    private static Avalonia.Controls.Window? GetWindow() => (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime)?.MainWindow;
    private void Clear() { NewFullName = NewBirthDate = NewPhone = ""; }
}
