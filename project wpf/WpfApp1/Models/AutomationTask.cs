using System;
using System.Collections.Generic;

namespace WpfApp1.Models;

public partial class AutomationTask
{
    public int TaskId { get; set; }

    public string? TaskName { get; set; }

    public string? Script { get; set; }

    public int? TargetDeviceId { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public string? Status { get; set; }

    public string? Output { get; set; }

    public virtual Device? TargetDevice { get; set; }
}
