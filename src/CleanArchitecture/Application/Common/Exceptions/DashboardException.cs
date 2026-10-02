using System.Diagnostics.CodeAnalysis;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Application.Common.Exceptions;

[ExcludeFromCodeCoverage]
public static class DashboardException
{
    public static UserFriendlyException BadRequestException(string errorMessage)
        => new(ErrorCode.BadRequest, errorMessage, errorMessage);
}
