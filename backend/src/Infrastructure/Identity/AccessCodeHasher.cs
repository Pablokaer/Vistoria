using InspectFlow.Modules.Inspections.Application;
using Microsoft.AspNetCore.Identity;

namespace InspectFlow.Infrastructure.Identity;

/// <summary>PBKDF2 (ASP.NET Core Identity v3 format, per-hash salt) for 6-digit access codes.</summary>
public sealed class AccessCodeHasher : IAccessCodeHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Subject = new();

    public string Hash(string code) => Hasher.HashPassword(Subject, code);

    public bool Verify(string hash, string code) =>
        Hasher.VerifyHashedPassword(Subject, hash, code) is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
