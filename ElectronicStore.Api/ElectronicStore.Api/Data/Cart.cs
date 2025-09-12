using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Cart
{
    public int CartId { get; set; }

    public int AccountId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();
}
