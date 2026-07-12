using System;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid GetCurrentUserId();
    string GetCurrentStringUserId();
    bool IsRememberMe();
}
