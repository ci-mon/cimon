﻿using System.Collections.Immutable;
using System.Security.Claims;

namespace Cimon.Contracts;

public record UserBase(UserName Name);
public record User(UserName Name, string FullName, IReadOnlyCollection<string> Teams, IReadOnlyCollection<string> Roles) : UserBase(Name)
{
	public static User Guest { get; } = Create("guest", "Guest");

	public string? Email => Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
	public IImmutableList<Claim> Claims { get; init; } = ImmutableList<Claim>.Empty;
	public string? DefaultMonitorId { get; set; }

	public virtual bool Equals(User? other) => base.Equals(other);
	public override int GetHashCode() => base.GetHashCode();

	public static User Create(UserName name, string fullName, IEnumerable<Claim>? claims = null) =>
		new(name, fullName, ImmutableList<string>.Empty, ImmutableList<string>.Empty) {
			Claims = claims?.ToImmutableList() ?? ImmutableList<Claim>.Empty
		};
}

public static class Utils
{
	public static bool IsGuest(this User? user) => user == User.Guest;
}
