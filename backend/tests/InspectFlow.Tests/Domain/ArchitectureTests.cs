using System.Reflection;
using InspectFlow.Modules.Common;

namespace InspectFlow.Tests.Domain;

/// <summary>Guards the modular-monolith boundaries: domain code must not depend on infrastructure or vendors.</summary>
public class ArchitectureTests
{
    private static readonly string[] ForbiddenAssemblies =
    [
        "Microsoft.EntityFrameworkCore", "Npgsql", "QuestPDF", "System.Net.Http", "Microsoft.AspNetCore.Http",
        "InspectFlow.Infrastructure", "InspectFlow.Api",
    ];

    [Fact]
    public void Domain_types_do_not_reference_infrastructure_or_vendor_types()
    {
        var domainTypes = typeof(IAppDbContext).Assembly.GetTypes()
            .Where(t => t.Namespace?.EndsWith(".Domain", StringComparison.Ordinal) == true).ToList();
        Assert.NotEmpty(domainTypes);

        var violations = new List<string>();
        foreach (var type in domainTypes)
        {
            var referenced = new List<Type>();
            if (type.BaseType is not null) referenced.Add(type.BaseType);
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            referenced.AddRange(type.GetFields(all).Select(f => f.FieldType));
            referenced.AddRange(type.GetProperties(all).Select(p => p.PropertyType));
            foreach (var m in type.GetMethods(all))
            {
                referenced.Add(m.ReturnType);
                referenced.AddRange(m.GetParameters().Select(p => p.ParameterType));
            }

            foreach (var t in referenced.SelectMany(Flatten))
            {
                var asm = t.Assembly.GetName().Name ?? string.Empty;
                if (ForbiddenAssemblies.Any(f => asm.StartsWith(f, StringComparison.Ordinal)))
                    violations.Add($"{type.FullName} -> {t.FullName} ({asm})");
            }
        }
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations.Distinct()));
    }

    [Fact]
    public void Modules_assembly_does_not_reference_infrastructure_vendors()
    {
        var refs = typeof(IAppDbContext).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
        Assert.DoesNotContain(refs, r => r.StartsWith("QuestPDF", StringComparison.Ordinal) || r.StartsWith("Npgsql", StringComparison.Ordinal) ||
                                         r.StartsWith("InspectFlow.Infrastructure", StringComparison.Ordinal));
    }

    private static IEnumerable<Type> Flatten(Type t)
    {
        yield return t;
        if (t.HasElementType) foreach (var e in Flatten(t.GetElementType()!)) yield return e;
        if (t.IsGenericType) foreach (var g in t.GetGenericArguments().SelectMany(Flatten)) yield return g;
    }
}
