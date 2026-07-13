namespace dotNet101.Application.Validation;

public sealed record ValidationIssue(
    IReadOnlyList<string> Loc,
    string Msg,
    string Type,
    object? Input = null
);
