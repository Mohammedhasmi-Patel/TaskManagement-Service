using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Domain.Enums;

public enum TaskStatusEnum
{
    Todo = 0,
    InProgress = 1,
    InReview = 2,
    Completed = 3,
    Blocked = 4,
    Cancelled = 5

}
