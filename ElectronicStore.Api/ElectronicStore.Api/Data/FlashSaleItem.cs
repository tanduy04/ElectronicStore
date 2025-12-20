using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class FlashSaleItem
{
    public int ItemId { get; set; }

    public int FlashSaleId { get; set; }

    public int ProductId { get; set; }

    public decimal SellPrice { get; set; }

    public int Quantity { get; set; }

    public virtual FlashSale FlashSale { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
