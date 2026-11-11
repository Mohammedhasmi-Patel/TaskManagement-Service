using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Domain.Enums;

public enum ProjectStatus
{
    Planned = 0,
    InProgress = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}
