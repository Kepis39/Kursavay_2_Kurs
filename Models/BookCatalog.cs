namespace LibraryApp.Models;

public partial class BookCatalog
{
    public int CatalogId { get; set; }
    public string BookId { get; set; } = null!;
    public int ThemeCode { get; set; }
    public virtual Book Book { get; set; } = null!;
    public virtual ThematicCatalog ThemeCodeNavigation { get; set; } = null!;
}
