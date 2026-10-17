namespace InspectFlow.Modules.Inspections.Domain;

public enum InspectionType
{
    MoveIn,
    MoveOut,
    Periodic,
    Other,
}

/// <summary>
/// Who may take an inspection. Agents only ever see inspections of the company that created their account
/// (<c>CompanyAgent</c>); there is no marketplace and no outside inspector (see ADR 0022).
/// </summary>
public enum InspectionVisibility
{
    /// <summary>Listed to every agent of the company under "Available"; the first to accept takes it.</summary>
    Public,

    /// <summary>The company chose the agent; only that agent sees it (Assigned on publish).</summary>
    Direct,
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
