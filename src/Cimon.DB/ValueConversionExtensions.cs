using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cimon.DB;

public static class ValueConversionExtensions
{
	static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
	static T? Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value);

	public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder)
	{
		ValueConverter<T?, string> converter = new(
			v => Serialize(v),
			v => Deserialize<T>(v)
		);

		ValueComparer<T?> comparer = new(
			(l, r) => Serialize(l) == Serialize(r),
			v => Serialize(v).GetHashCode(),
			v => Deserialize<T>(Serialize(v))
		);

		propertyBuilder.HasConversion(converter!);
		propertyBuilder.Metadata.IsNullable = true;
		propertyBuilder.Metadata.SetValueConverter(converter);
		propertyBuilder.Metadata.SetValueComparer(comparer);
		propertyBuilder.HasColumnType("jsonb");
		return propertyBuilder;
	}
}
