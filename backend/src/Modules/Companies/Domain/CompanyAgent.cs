namespace InspectFlow.Modules.Companies.Domain;

/// <summary>
/// An agent (inspector) registered by a company as part of its own team. Team agents see the company's
/// <c>Team</c> inspections and can be assigned an inspection directly. An agent may belong to several
/// companies; independent agents (no link) keep using the public marketplace and private invitations.
/// </summary>
public class CompanyAgent
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid AgentUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}
