#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class CVarIntegrityTest
{
    [Test]
    public void TestAllCVarsHaveUniqueAndValidNames()
    {
        var fields = typeof(CVars).GetFields(BindingFlags.Public | BindingFlags.Static);
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.That(fields.Length, Is.GreaterThan(20), "Expected substantial number of CVars");

        foreach (var field in fields)
        {
            if (!typeof(CVarDef).IsAssignableFrom(field.FieldType))
                continue;

            var cvar = (CVarDef?)field.GetValue(null);
            Assert.That(cvar, Is.Not.Null, $"CVar field {field.Name} should not be null");
            Assert.That(cvar!.Name, Is.Not.Null.And.Not.Empty, $"CVar field {field.Name} has empty name");

            Assert.That(seenNames.Add(cvar.Name), Is.True, $"Duplicate CVar name detected: '{cvar.Name}' in field {field.Name}");
            Assert.That(cvar.ValueType, Is.Not.Null, $"CVar {cvar.Name} has null ValueType");
        }
    }

    [Test]
    public void TestCVarDefaultValuesMatchDeclaredTypes()
    {
        var fields = typeof(CVars).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            if (!typeof(CVarDef).IsAssignableFrom(field.FieldType))
                continue;

            var cvar = (CVarDef)field.GetValue(null)!;

            if (cvar.DefaultValue != null)
            {
                Assert.That(cvar.ValueType.IsInstanceOfType(cvar.DefaultValue), Is.True,
                    $"CVar {cvar.Name} DefaultValue '{cvar.DefaultValue}' is not of declared type {cvar.ValueType}");
            }
        }
    }
}
