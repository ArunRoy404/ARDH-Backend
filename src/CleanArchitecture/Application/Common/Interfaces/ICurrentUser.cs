using System;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid GetCurrentUserId();
    string GetCurrentStringUserId();
    bool IsRememberMe();

    /// <summary>
    /// Overrides the current-user id for the lifetime of this scope. Used by background jobs
    /// (e.g. bulk upload) that run outside an HTTP request and therefore have no HttpContext to
    /// resolve the user from.
    /// </summary>
    void SetCurrentUserId(Guid userId);
}
