using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Customer
{
    public int CustomerId { get; set; }

    public int AccountId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Address { get; set; }

    public DateOnly? BirthDate { get; set; }

    public byte? Gender { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
