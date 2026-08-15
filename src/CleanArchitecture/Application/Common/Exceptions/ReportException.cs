namespace CleanArchitecture.Application.Common.Exceptions;

public static class ReportException
{
    public static UserFriendlyException BadRequestException(string errorMessage)
        => new(ErrorCode.BadRequest, errorMessage, errorMessage);
}
