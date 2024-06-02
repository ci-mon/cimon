using Microsoft.AspNetCore.Components;
using Radzen;

namespace Cimon.Shared;

public enum GravatarStyle
{
	/// <summary>
	/// Do not load any image if none is associated with the email hash, instead return an HTTP 404 (File Not Found) response
	/// </summary>
	NotFound,
	/// <summary>
	/// (mystery-person) a simple, cartoon-style silhouetted outline of a person (does not vary by email hash)
	/// </summary>
	Mp,
	/// <summary>
	/// A geometric pattern based on an email hash
	/// </summary>
	Identicon,
	/// <summary>
	/// A generated ‘monster’ with different colors, faces, etc
	/// </summary>
	Monsterid,
	/// <summary>
	/// Generated faces with differing features and backgrounds
	/// </summary>
	Wavatar,
	/// <
	/// summary>
	/// Awesome generated, 8-bit arcade-style pixelated faces
	/// </summary>
	Retro,
	/// <summary>
	/// A generated robot with different colors, faces, etc
	/// </summary>
	Robohash,
	/// <summary>
	/// A transparent PNG image
	/// </summary>
	Blank,
}

public partial class StyledGravatar : RadzenComponent
{
	/// <summary>
	/// Gets or sets the email.
	/// </summary>
	/// <value>The email.</value>
	[Parameter]
	public string? Email { get; set; }

	/// <summary>
	/// Gets or sets the text.
	/// </summary>
	/// <value>The text.</value>
	[Parameter]
	public string AlternateText { get; set; } = "gravatar";

	[Parameter]
	public GravatarStyle GravatarStyle { get; set; } = GravatarStyle.Retro;

	/// <summary>
	/// Gets gravatar URL.
	/// </summary>
	protected string Url
	{
		get
		{
			var md5Email = MD5.Calculate(System.Text.Encoding.ASCII.GetBytes(Email ?? ""));
			var style = GravatarStyle switch {
				GravatarStyle.NotFound => "404",
				var value => value.ToString().ToLowerInvariant()
			};
			const string width = "36";
			return $"https://secure.gravatar.com/avatar/{md5Email}?d={style}&s={width}";
		}
	}

	string GetAlternateText()
	{
		if (Attributes != null && Attributes.TryGetValue("alt", out var @alt) && !string.IsNullOrEmpty(Convert.ToString(@alt)))
		{
			return $"{AlternateText} {@alt}";
		}

		return AlternateText;
	}

	/// <inheritdoc />
	protected override string GetComponentCssClass()
	{
		return "rz-gravatar";
	}
}
