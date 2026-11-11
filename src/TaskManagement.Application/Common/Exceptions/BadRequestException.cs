using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Application.Common.Exceptions;

public class BadRequestException : AppException
{
    public BadRequestException(string message = "Invalid data provide") : base(message)
    {
    }
}
