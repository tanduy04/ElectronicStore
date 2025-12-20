using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class ProductReview
{
    public int ReviewId { get; set; }

    public int ProductId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public int Rating { get; set; }

    public int? ParentId { get; set; }

    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual Product Product { get; set; } = null!;
}
