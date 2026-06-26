using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LibraryApp.Views;

public partial class BooksView : UserControl
{
    public BooksView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
