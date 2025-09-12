using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class AccountToken
{
    public int TokenId { get; set; }

    public int AccountId { get; set; }

    public string RefreshToken { get; set; } = null!;

    public string? DeviceInfo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiryDate { get; set; }

    public virtual Account Account { get; set; } = null!;
}
