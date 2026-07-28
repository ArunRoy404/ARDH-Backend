using System.Diagnostics.CodeAnalysis;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Application.Common.Exceptions;

[ExcludeFromCodeCoverage]
public static class OwnerException
{
    public static UserFriendlyException NotFoundException(string errorMessage)
        => new(ErrorCode.NotFound, errorMessage, errorMessage);

    public static UserFriendlyException BadRequestException(string errorMessage)
        => new(ErrorCode.BadRequest, errorMessage, errorMessage);
}
