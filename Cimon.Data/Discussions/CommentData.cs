﻿using Cimon.Data.Users;

namespace Cimon.Data;

public class CommentData
{
	public User Author { get; set; }
	public string Comment { get; set; } = string.Empty;
}