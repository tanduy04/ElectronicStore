using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Import
{
    public int ImportId { get; set; }

    public string ImportCode { get; set; } = null!;

    public string? Supplier { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime ImportDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
}
