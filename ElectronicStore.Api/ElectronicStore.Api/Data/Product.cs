using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int? ConsumptionCapacity { get; set; }

    public int? Maintenance { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? DiscountPrice { get; set; }

    public decimal? SellPrice { get; set; }

    public int? StockQuantity { get; set; }

    public int? CategoryId { get; set; }

    public int? ManufactureYear { get; set; }

    public int? BrandId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<FavoriteProduct> FavoriteProducts { get; set; } = new List<FavoriteProduct>();

    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
}
