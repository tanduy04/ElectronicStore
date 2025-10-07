using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Import
{
    public int ImportId { get; set; }

    public string ImportCode { get; set; } = null!;

    public int SupplierId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime ImportDate { get; set; }

    public string? Status { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();

    public virtual Supplier ImportNavigation { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}
