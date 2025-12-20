using System;
using System.Collections.Generic;

namespace ElectronicStore.Api.Data;

public partial class QuestionAndAnswer
{
    public int Id { get; set; }

    public string? Question { get; set; }

    public string? Answer { get; set; }
}
