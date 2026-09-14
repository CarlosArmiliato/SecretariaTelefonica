namespace Joana.Desktop.ViewModels;
public sealed class DialogueViewModel { public bool IsBlocked { get; set; } public IReadOnlyList<string> AllowedFacts { get; init; } = []; }
