using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class ImportDetail
{
    public int ImportDetailId { get; set; }

    public int ImportId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Import Import { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
