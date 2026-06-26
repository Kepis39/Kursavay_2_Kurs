using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryApp.Models;
using LibraryApp.Services;
using MsBox.Avalonia;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;

namespace LibraryApp.ViewModels;

public partial class LoansViewModel : ViewModelBase
{
    private readonly DataService _data = new();
    private readonly ImportService _import = new();

    [ObservableProperty] private ObservableCollection<DistributionToReader> _loans = new();
    [ObservableProperty] private DistributionToReader? _selectedLoan;
    [ObservableProperty] private string _newReaderId = "";
    [ObservableProperty] private string _newInventoryNumber = "";
    [ObservableProperty] private string _newIssueDate = "";
    [ObservableProperty] private string _newReturnDate = "";
    [ObservableProperty] private string _status = "";

    public LoansViewModel()
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await Load();
    }

    [RelayCommand] private async Task Load()
    {
        Loans = new ObservableCollection<DistributionToReader>(await _data.GetLoansAsync());
        Status = $"Loans: {Loans.Count}";
    }

    [RelayCommand] private async Task Add()
    {
        if (string.IsNullOrWhiteSpace(NewInventoryNumber)) { Status = "Enter Inv#"; return; }

        var issueDate = ParseDate(NewIssueDate);
        var returnDate = ParseDate(NewReturnDate);
        if (NewIssueDate != "" && issueDate == null) { Status = "Invalid Issue Date (yyyy-MM-dd)"; return; }
        if (NewReturnDate != "" && returnDate == null) { Status = "Invalid Return Date (yyyy-MM-dd)"; return; }

        int? readerId = string.IsNullOrWhiteSpace(NewReaderId) ? null : (int.TryParse(NewReaderId, out var r) ? r : null);
        var l = new DistributionToReader 
        { 
            TicketNumber = readerId, 
            InventoryNumber = NewInventoryNumber, 
            IssueDate = issueDate,
            ReturnDate = returnDate
        };
        if (await _data.AddLoanAsync(l)) { Clear(); await Load(); Status = "Loan added"; }
        else Status = "Error";
    }

    [RelayCommand] private async Task Delete()
    {
        if (SelectedLoan == null) return;
        var box = MessageBoxManager.GetMessageBoxStandard("Confirm", "Delete loan?", MsBox.Avalonia.Enums.ButtonEnum.YesNo);
        if (await box.ShowAsync() == MsBox.Avalonia.Enums.ButtonResult.Yes && await _data.DeleteLoanAsync(SelectedLoan.TicketNumber ?? 0, SelectedLoan.InventoryNumber ?? "", SelectedLoan.IssueDate))
        { await Load(); Status = "Deleted"; }
    }

    [RelayCommand] private async Task ImportExcel() => await PickAndImport("xlsx", _import.ImportLoansFromExcelAsync);

    partial void OnSelectedLoanChanged(DistributionToReader? value)
    {
        if (value != null)
        { 
            NewReaderId = value.TicketNumber?.ToString() ?? ""; 
            NewInventoryNumber = value.InventoryNumber ?? ""; 
            NewIssueDate = value.IssueDate?.ToString("yyyy-MM-dd") ?? ""; 
            NewReturnDate = value.ReturnDate?.ToString("yyyy-MM-dd") ?? ""; 
        }
    }

    private static DateTime? ParseDate(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

    private async Task PickAndImport(string ext, Func<string, Task<bool>> action)
    {
        var w = GetWindow(); if (w == null) return;
        var files = await w.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType(ext.ToUpper()) { Patterns = new[] { $"*.{ext}" } } } });
        if (files.Count > 0) { var path = files[0].TryGetLocalPath(); if (string.IsNullOrEmpty(path)) { Status = "No path"; return; } if (await action(path)) { await Load(); Status = "Import OK"; } else Status = $"Import failed: {string.Join(", ", _import.ImportErrors)}"; }
    }

    private static Avalonia.Controls.Window? GetWindow() => (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime)?.MainWindow;
    private void Clear() { NewReaderId = NewInventoryNumber = NewIssueDate = NewReturnDate = ""; }
}
