using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class AccountLoginProvider
{
    public int ProviderId { get; set; }

    public int AccountId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderKey { get; set; } = null!;

    public string? ProviderEmail { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
