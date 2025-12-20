using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class Banner
{
    public int BannerId { get; set; }

    public string? BannerName { get; set; }

    public string? ImageUrl { get; set; }
}
