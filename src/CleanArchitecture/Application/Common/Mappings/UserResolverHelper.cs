using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Common.Mappings;

public static class UserResolverHelper
{
    /// <summary>
    /// Resolves user names from a collection of nullable user IDs.
    /// Returns a dictionary mapping user ID to user name.
    /// </summary>
    public static async Task<Dictionary<Guid, string>> ResolveUserNamesAsync(
        IUserRepository userRepository,
        IEnumerable<Guid?> userIds,
        CancellationToken cancellationToken = default)
    {
        var distinctIds = userIds
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var result = new Dictionary<Guid, string>();

        foreach (var userId in distinctIds)
        {
            var user = await userRepository.FirstOrDefaultAsync(x => x.Id == userId);
            if (user != null)
            {
                result[userId] = user.Name;
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves a single user's name from their ID.
    /// </summary>
    public static async Task<string?> ResolveUserNameAsync(
        IUserRepository userRepository,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue || userId.Value == Guid.Empty)
            return null;

        var user = await userRepository.FirstOrDefaultAsync(x => x.Id == userId.Value);
        return user?.Name;
    }
}
