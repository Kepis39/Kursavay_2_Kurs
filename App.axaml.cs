using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LibraryApp.Models;
using LibraryApp.ViewModels;
using LibraryApp.Views;
using SQLitePCL;
using System;

namespace LibraryApp;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Инициализация SQLite (обязательно для SQLitePCLRaw)
        Batteries.Init();

        using var ctx = new LibraryabonementContext();
        ctx.Database.EnsureCreated();

        if (ApplicationLifetime is ClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = new MainViewModel() };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
