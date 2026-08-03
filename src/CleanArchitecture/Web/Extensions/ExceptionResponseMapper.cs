using System.Net;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Shared.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Web.Extensions;

/// <summary>
/// Central mapping of exceptions to HTTP status codes, user-friendly messages
/// and error codes so every exception handler in the app behaves identically.
/// </summary>
public static class ExceptionResponseMapper
{
    public static (int StatusCode, string Message, string ErrorCode) Map(Exception exception)
    {
        if (exception is UserFriendlyException userFriendly)
        {
            return MapUserFriendly(userFriendly);
        }

        // Unwrap database errors (unique constraint, foreign-key conflict, null, truncation, ...)
        if (exception is DbUpdateException || exception is SqlException)
        {
            var (message, errorCode) = DbErrorResolver.Resolve(exception);
            return MapWithCode(errorCode, message);
        }

        return ((int)HttpStatusCode.InternalServerError,
            "An unexpected error occurred. Please try again later.",
            $"{ApplicationConstants.Name}.{ErrorRespondCode.GENERAL_ERROR}");
    }

    private static (int StatusCode, string Message, string ErrorCode) MapUserFriendly(UserFriendlyException exception)
    {
        var message = exception.UserFriendlyMessage;
        return exception.ErrorCode switch
        {
            ErrorCode.NotFound => ((int)HttpStatusCode.NotFound, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.NOT_FOUND}"),
            ErrorCode.VersionConflict => ((int)HttpStatusCode.Conflict, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.VERSION_CONFLICT}"),
            ErrorCode.ItemAlreadyExists => ((int)HttpStatusCode.Conflict, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.ITEM_ALREADY_EXISTS}"),
            ErrorCode.Conflict => ((int)HttpStatusCode.Conflict, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.CONFLICT}"),
            ErrorCode.BadRequest => ((int)HttpStatusCode.BadRequest, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}"),
            ErrorCode.Unauthorized => ((int)HttpStatusCode.Unauthorized, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.UNAUTHORIZED}"),
            ErrorCode.Internal => ((int)HttpStatusCode.InternalServerError, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.INTERNAL_ERROR}"),
            ErrorCode.UnprocessableEntity => ((int)HttpStatusCode.UnprocessableEntity, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.UNPROCESSABLE_ENTITY}"),
            _ => ((int)HttpStatusCode.InternalServerError, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.GENERAL_ERROR}")
        };
    }

    private static (int StatusCode, string Message, string ErrorCode) MapWithCode(ErrorCode errorCode, string message)
        => errorCode switch
        {
            ErrorCode.NotFound => ((int)HttpStatusCode.NotFound, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.NOT_FOUND}"),
            ErrorCode.BadRequest => ((int)HttpStatusCode.BadRequest, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}"),
            ErrorCode.Unauthorized => ((int)HttpStatusCode.Unauthorized, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.UNAUTHORIZED}"),
            _ => ((int)HttpStatusCode.InternalServerError, message, $"{ApplicationConstants.Name}.{ErrorRespondCode.INTERNAL_ERROR}")
        };
}
