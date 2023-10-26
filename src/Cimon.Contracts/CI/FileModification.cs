namespace Cimon.Contracts.CI;

public record struct FileModification(FileModificationType Type, string Path);