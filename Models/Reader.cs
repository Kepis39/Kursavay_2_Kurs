using System;
using System.Collections.Generic;

namespace LibraryApp.Models;

public partial class Reader
{
    public int TicketNumber { get; set; }
    public string FullName { get; set; } = null!;
    public DateTime? BirthDate { get; set; }
    public string? Phone { get; set; }
    public virtual ICollection<DistributionToReader> Issues { get; set; } = new List<DistributionToReader>();
}
