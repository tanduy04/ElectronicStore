using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Supplier
{
    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public virtual Import? ImportImportNavigation { get; set; }

    public virtual ICollection<Import> ImportSuppliers { get; set; } = new List<Import>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
