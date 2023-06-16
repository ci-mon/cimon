using System.Security.Claims;

namespace Cimon.Data.Users;

public delegate Task<ClaimsPrincipal?> GetCurrentPrincipal();