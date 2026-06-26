using System.Collections.Generic;

namespace LibraryApp.Models;

public partial class ThematicCatalog
{
    public int ThemeCode { get; set; }
    public string ThemeName { get; set; } = null!;
    public virtual ICollection<BookCatalog> BookCatalogs { get; set; } = new List<BookCatalog>();
    public virtual ICollection<Copy> Copies { get; set; } = new List<Copy>();
}
