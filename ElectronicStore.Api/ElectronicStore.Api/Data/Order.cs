using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Order
{
    public int OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public int CustomerId { get; set; }

    public int? EmployeeId { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string ShippingAddress { get; set; } = null!;

    public string? Note { get; set; }

    public decimal? DiscountVoucher { get; set; }

    public string? VoucherCode { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
