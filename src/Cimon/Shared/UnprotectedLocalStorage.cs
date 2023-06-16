using System.Text.Json;
using Microsoft.JSInterop;
using Optional;
using Optional.Linq;

namespace Cimon.Shared;

class UnprotectedLocalStorage
{
	private readonly IJSRuntime _jsRuntime;
	private readonly string _storeName = "localStorage";

	private readonly JsonSerializerOptions _serializerOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
	};


	public UnprotectedLocalStorage(IJSRuntime jsRuntime) {
		_jsRuntime = jsRuntime;
	}

	public ValueTask SetAsync(string key, object value) {
		var json = JsonSerializer.Serialize(value, _serializerOptions);
		return _jsRuntime.InvokeVoidAsync($"{_storeName}.setItem", key, json);
	}

	public async Task<Option<TValue>> GetAsync<TValue>(string key) {
		var json = await _jsRuntime.InvokeAsync<string>($"{_storeName}.getItem", key);
		return json.Some().Where(x => !string.IsNullOrWhiteSpace(x))
			.Map(x => JsonSerializer.Deserialize<TValue>(x, _serializerOptions))!;
	}
}