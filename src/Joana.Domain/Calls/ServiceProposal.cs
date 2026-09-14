namespace Joana.Domain.Calls;

public enum ProposalOrigin { CallerConfirmed, CallerEstimate, AgentInference }
public sealed record ServiceProposal(ProposalOrigin Origin, string? Diagnosis = null, string? Total = null, string? Labor = null, string? Parts = null, string? Travel = null, string? Conditions = null, string? ServiceDeadline = null, string? CompletionDeadline = null, string? Warranty = null, string? Validity = null, string? ServiceMethod = null, string? Protocol = null, string? NextSteps = null);
