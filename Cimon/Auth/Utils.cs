using Cimon.Contracts;

namespace Cimon.Auth;

using System.Security.Principal;

public static class Utils
{
	public static string GetUserName(this IIdentity? source) {
		return ((UserName)source?.Name).Name;
	}
}
