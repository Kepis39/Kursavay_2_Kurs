using CommunityToolkit.Mvvm.ComponentModel;

namespace LibraryApp.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private string _welcomeText = "Library Management System";
}
