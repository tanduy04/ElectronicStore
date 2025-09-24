using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string Method { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public DateTime? PaymentDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
