using System.Diagnostics.CodeAnalysis;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Application.Common.Exceptions;

[ExcludeFromCodeCoverage]
public static class ApartmentException
{
    public static UserFriendlyException NotFoundException(string errorMessage = "The specified apartment does not exist.")
        => new(ErrorCode.NotFound, errorMessage, errorMessage);

    public static UserFriendlyException BadRequestException(string errorMessage = "Invalid request for apartment.")
        => new(ErrorCode.BadRequest, errorMessage, errorMessage);
}
