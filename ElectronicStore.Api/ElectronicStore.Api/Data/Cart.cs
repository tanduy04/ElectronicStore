using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Cart
{
    public int CartId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public virtual Account CartNavigation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
