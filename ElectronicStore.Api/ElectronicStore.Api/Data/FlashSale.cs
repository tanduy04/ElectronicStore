using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class FlashSale
{
    public int FlashSaleId { get; set; }

    public string? FlashSaleName { get; set; }

    public string? Description { get; set; }

    public DateOnly DateSale { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual ICollection<FlashSaleItem> FlashSaleItems { get; set; } = new List<FlashSaleItem>();
}
