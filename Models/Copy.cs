using System.Collections.Generic;

namespace LibraryApp.Models;

public partial class Copy
{
    public string InventoryNumber { get; set; } = null!;
    public string BookId { get; set; } = null!;
    public int? CopyCount { get; set; }
    public int? ThemeCode { get; set; }
    public virtual Book Book { get; set; } = null!;
    public virtual ICollection<DistributionToReader> Issues { get; set; } = new List<DistributionToReader>();
    public virtual ThematicCatalog? ThemeCodeNavigation { get; set; }
}
