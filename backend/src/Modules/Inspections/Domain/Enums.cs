namespace InspectFlow.Modules.Inspections.Domain;

public enum InspectionType
{
    MoveIn,
    MoveOut,
    Periodic,
    Other,
}

public enum InspectionVisibility
{
    /// <summary>Listed in the agent marketplace ("Available Inspections").</summary>
    Public,

    /// <summary>Only reachable through an invitation link plus a 6-digit access code.</summary>
    Private,

    /// <summary>
    /// Listed only to the agents the company registered in its team (<c>CompanyAgent</c>); the first to accept
    /// takes it. The company may instead assign it directly to one of them.
    /// </summary>
    Team,
}

public enum InspectionStatus
{
    Draft,
    Open,
    Assigned,
    InProgress,
    Review,
    Completed,
    AwaitingTenant,
    Accepted,
    Disputed,
    Cancelled,
    Expired,
}

public enum InspectionRoomStatus
{
    Pending,
    InProgress,
    Completed,
}

/// <summary>How a recorded defect relates to the property's history. Not all values are meaningful for Move In.</summary>
public enum DefectClassification
{
    Unknown,
    NewDamage,
    PreExisting,
    NormalWear,
    Resolved,
    Unchanged,
}

/// <summary>The agent's (human) decision for a Move Out room compared with its Move In baseline.</summary>
public enum ComparisonDecision
{
    NewDamage,
    PreExisting,
    NormalWear,
    Resolved,
    Unchanged,
    UnableToDetermine,
}

public enum DescriptionSource
{
    None,
    Ai,
    Agent,
}
