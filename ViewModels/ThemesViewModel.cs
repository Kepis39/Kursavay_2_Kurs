using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryApp.Models;
using LibraryApp.Services;
using MsBox.Avalonia;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LibraryApp.ViewModels;

public partial class ThemesViewModel : ViewModelBase
{
    private readonly DataService _data = new();
    private readonly ImportService _import = new();

    [ObservableProperty] private ObservableCollection<ThematicCatalog> _themes = new();
    [ObservableProperty] private ThematicCatalog? _selectedTheme;
    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _status = "";

    public ThemesViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await Load();
    }

    [RelayCommand] private async Task Load()
    {
        Themes = new ObservableCollection<ThematicCatalog>(await _data.GetThemesAsync());
        Status = $"Themes: {Themes.Count}";
    }

    [RelayCommand] private async Task Add()
    {
        if (string.IsNullOrWhiteSpace(NewName)) { Status = "Enter Name"; return; }
        var t = new ThematicCatalog { ThemeName = NewName.Trim() };
        if (await _data.AddThemeAsync(t)) { Clear(); await Load(); Status = "Theme added"; }
        else Status = "Error";
    }

    [RelayCommand] private async Task Update()
    {
        if (SelectedTheme == null) return;
        SelectedTheme.ThemeName = NewName;
        if (await _data.UpdateThemeAsync(SelectedTheme)) { await Load(); Status = "Updated"; }
        else Status = "Error";
    }

    [RelayCommand] private async Task Delete()
    {
        if (SelectedTheme == null) return;
        var box = MessageBoxManager.GetMessageBoxStandard("Confirm", $"Delete {SelectedTheme.ThemeName}?", MsBox.Avalonia.Enums.ButtonEnum.YesNo);
        if (await box.ShowAsync() == MsBox.Avalonia.Enums.ButtonResult.Yes && await _data.DeleteThemeAsync(SelectedTheme.ThemeCode))
        { await Load(); Status = "Deleted"; }
    }

    [RelayCommand] private async Task ImportCsv() => await PickAndImport("csv", _import.ImportThemesFromCsvAsync);

    partial void OnSelectedThemeChanged(ThematicCatalog? value)
    {
        if (value != null) NewName = value.ThemeName;
    }

    private async Task PickAndImport(string ext, System.Func<string, Task<bool>> action)
    {
        var w = GetWindow(); if (w == null) return;
        var files = await w.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType(ext.ToUpper()) { Patterns = new[] { $"*.{ext}" } } } });
        if (files.Count > 0) { var path = files[0].TryGetLocalPath(); if (string.IsNullOrEmpty(path)) { Status = "No path"; return; } if (await action(path)) { await Load(); Status = "Import OK"; } else Status = $"Import failed: {string.Join(", ", _import.ImportErrors)}"; }
    }

    private static Avalonia.Controls.Window? GetWindow() => (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime)?.MainWindow;
    private void Clear() { NewName = ""; }
}
