// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Describes one explicitly allowed value or function exposed to a script.</summary>
public sealed record ScriptBinding
{
    private ScriptBinding(string name, object? value, Delegate? function)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            !name.All(static character => char.IsLetterOrDigit(character) || character == '_') ||
            char.IsDigit(name[0]))
            throw new ArgumentException("Binding names must be portable script identifiers.", nameof(name));
        Name = name;
        Value = value;
        Function = function;
    }

    /// <summary>Gets the binding name.</summary>
    public string Name { get; }
    /// <summary>Gets the scalar value.</summary>
    public object? Value { get; }
    /// <summary>Gets the explicitly callable function.</summary>
    public Delegate? Function { get; }

    /// <summary>Creates a scalar binding.</summary>
    public static ScriptBinding ForValue(string name, object? value)
    {
        if (value is not null and not string and not bool and not char and
            not sbyte and not byte and not short and not ushort and not int and not uint and
            not long and not ulong and not float and not double and not decimal)
            throw new ArgumentException("Only scalar values may cross a script boundary.", nameof(value));
        return new ScriptBinding(name, value, null);
    }

    /// <summary>Creates an in-process capability function binding.</summary>
    public static ScriptBinding ForFunction(string name, Delegate function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return new ScriptBinding(name, null, function);
    }
}
