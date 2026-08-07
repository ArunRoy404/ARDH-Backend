using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Application.Common.Exceptions;

public static class BulkUploadException
{
    public static UserFriendlyException NotFoundException(string errorMessage)
        => new(ErrorCode.NotFound, errorMessage, errorMessage);

    public static UserFriendlyException BadRequestException(string errorMessage)
        => new(ErrorCode.BadRequest, errorMessage, errorMessage);
}
