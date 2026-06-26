using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LibraryApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private object _currentViewModel = new HomeViewModel();
    [ObservableProperty] private bool _isHomeSelected = true;
    [ObservableProperty] private bool _isBooksSelected;
    [ObservableProperty] private bool _isCopiesSelected;
    [ObservableProperty] private bool _isReadersSelected;
    [ObservableProperty] private bool _isThemesSelected;
    [ObservableProperty] private bool _isLoansSelected;
    [ObservableProperty] private bool _isReportsSelected;
    [ObservableProperty] private bool _isChartsSelected;
    [ObservableProperty] private bool _isImportExportSelected;

    private void Reset() { IsHomeSelected = IsBooksSelected = IsCopiesSelected = IsReadersSelected = IsThemesSelected = IsLoansSelected = IsReportsSelected = IsChartsSelected = IsImportExportSelected = false; }

    [RelayCommand] private void NavigateHome() { Reset(); IsHomeSelected = true; CurrentViewModel = new HomeViewModel(); }
    [RelayCommand] private void NavigateBooks() { Reset(); IsBooksSelected = true; CurrentViewModel = new BooksViewModel(); }
    [RelayCommand] private void NavigateCopies() { Reset(); IsCopiesSelected = true; CurrentViewModel = new CopiesViewModel(); }
    [RelayCommand] private void NavigateReaders() { Reset(); IsReadersSelected = true; CurrentViewModel = new ReadersViewModel(); }
    [RelayCommand] private void NavigateThemes() { Reset(); IsThemesSelected = true; CurrentViewModel = new ThemesViewModel(); }
    [RelayCommand] private void NavigateLoans() { Reset(); IsLoansSelected = true; CurrentViewModel = new LoansViewModel(); }
    [RelayCommand] private void NavigateReports() { Reset(); IsReportsSelected = true; CurrentViewModel = new ReportsViewModel(); }
    [RelayCommand] private void NavigateCharts() { Reset(); IsChartsSelected = true; CurrentViewModel = new ChartsViewModel(); }
    [RelayCommand] private void NavigateImportExport() { Reset(); IsImportExportSelected = true; CurrentViewModel = new ImportExportViewModel(); }
}
