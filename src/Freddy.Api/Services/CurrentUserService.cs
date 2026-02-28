using System.Security.Claims;
using Freddy.Application.Common.Interfaces;

namespace Freddy.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private static readonly Guid DevUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid UserId
    {
        get
        {
            string? userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(userIdClaim, out Guid userId) ? userId : DevUserId;
        }
    }
}
