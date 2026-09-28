using MS.SS.Core.Modules.Identity.Application.Dtos;
using MS.SS.Core.Modules.Identity.Application.Interfaces;
using MS.SS.Core.SharedKernel.Common.Exceptions;
using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Modules.Identity.Application.Queries.UserQueries;

/// <param name="AccessTokenExpiresAt">The <c>exp</c> of the presented token, so the answer has the same shape as sign-in.</param>
public sealed record GetCurrentUserQuery(Guid UserId, DateTime AccessTokenExpiresAt);

public sealed class GetCurrentUserQueryHandler(IUserRepository users)
{
    public async Task<Result<SessionResponse>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(query.UserId, cancellationToken);

        return user is { IsActive: true }
            ? SessionResponse.FromUser(user, query.AccessTokenExpiresAt)
            : new UnauthenticatedException("Not authenticated.");
    }
}
