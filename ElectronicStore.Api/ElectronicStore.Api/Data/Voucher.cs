using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string VoucherCode { get; set; } = null!;

    public string? VoucherName { get; set; }

    public string? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    public int? Quantity { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
