namespace Cimon.Contracts;

public enum MentionedEntityType { User, Team}

public readonly record struct MentionedEntityId(string Name, MentionedEntityType Type);
