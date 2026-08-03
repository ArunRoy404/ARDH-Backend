using System.Diagnostics.CodeAnalysis;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Application.Common.Exceptions;

[ExcludeFromCodeCoverage]
public static class TransactionException
{
    public static UserFriendlyException TransactionNotCommitException()
        => throw new UserFriendlyException(ErrorCode.Internal, ErrorMessage.TransactionNotCommit, ErrorMessage.TransactionNotCommit);

    public static UserFriendlyException TransactionNotExecuteException(Exception ex)
    {
        // Unwrap the underlying database error so the client receives a meaningful message.
        var (message, errorCode) = DbErrorResolver.Resolve(ex);
        return new UserFriendlyException(errorCode, message, message, ex);
    }
}
