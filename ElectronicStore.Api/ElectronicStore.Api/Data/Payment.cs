using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public int? AccountId { get; set; }

    public decimal Amount { get; set; }

    public string? Method { get; set; }

    public string? TransactionCode { get; set; }

    public DateTime PaymentDate { get; set; }

    public virtual Account? Account { get; set; }

    public virtual Order Order { get; set; } = null!;
}
