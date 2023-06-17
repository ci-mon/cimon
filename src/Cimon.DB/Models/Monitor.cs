﻿namespace Cimon.DB.Models;

public class Monitor
{
	public int Id { get; set; }
	public required string Key { get; set; }
	public string? Title { get; set; }
	public bool Removed { get; set; }
	public bool AlwaysOnMonitoring { get; set; }
	public List<BuildConfig> Builds { get; set; } = new();
}