namespace InspectFlow.Modules.Inspections.Application;

/// <summary>Slow, salted hashing for low-entropy secrets (6-digit access codes).</summary>
public interface IAccessCodeHasher
{
    string Hash(string code);
    bool Verify(string hash, string code);
}
