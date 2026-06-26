using System;

namespace LibraryApp.Models;

public partial class DistributionToReader
{
    public int IssueId { get; set; }
    public int? TicketNumber { get; set; }
    public string? InventoryNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public DateTime? ActualReturn { get; set; }
    public virtual Copy? InventoryNumberNavigation { get; set; }
    public virtual Reader? TicketNumberNavigation { get; set; }
}
