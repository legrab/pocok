// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pocok.Showcase.Components;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Licensing.Models;

namespace Pocok.Showcase.Licensing;

public partial class LicensingEditor
{
    private ShowcaseBufferedTextArea? _licensedModulesEditor;

    [Parameter][EditorRequired] public LicensingInput Value { get; set; } = new();

    [Parameter] public EventCallback<LicensingInput> ValueChanged { get; set; }

    [Parameter][EditorRequired] public IShowcaseText Text { get; set; } = default!;

    private static string ReadString(ChangeEventArgs args)
    {
        return args.Value?.ToString() ?? string.Empty;
    }

    private static T ReadValue<T>(ChangeEventArgs args, T fallback)
    {
        return BindConverter.TryConvertTo(args.Value, CultureInfo.InvariantCulture, out T? value) && value is not null
            ? value
            : fallback;
    }

    private string T(string key)
    {
        return Text.GetText("licensing", key);
    }

    /// <summary>Flushes pending browser-owned text before an explicit action.</summary>
    public async Task FlushAsync()
    {
        if (_licensedModulesEditor is not null)
            await _licensedModulesEditor.FlushAsync();
    }

    private Task SetLicenseIdAsync(string value)
    {
        return UpdateAsync(Value with { LicenseId = value });
    }

    private Task SetCustomerAsync(string value)
    {
        return UpdateAsync(Value with { Customer = value });
    }

    private Task SetIssuedAtAsync(string value)
    {
        return UpdateAsync(Value with { IssuedAtUtc = value });
    }

    private Task SetValidFromAsync(string value)
    {
        return UpdateAsync(Value with { ValidFromUtc = value });
    }

    private Task SetValidUntilAsync(string value)
    {
        return UpdateAsync(Value with { ValidUntilUtc = value });
    }

    private Task SetMaximumRuntimeAsync(int value)
    {
        return UpdateAsync(Value with { MaximumProcessRuntimeMinutes = Math.Clamp(value, 0, 10_080) });
    }

    private Task SetAllModulesAsync(bool value)
    {
        return UpdateAsync(Value with { AllModules = value });
    }

    private Task SetLicensedModulesAsync(string value)
    {
        return UpdateAsync(Value with { LicensedModules = value });
    }

    private Task SetRequiredModuleAsync(string value)
    {
        return UpdateAsync(Value with { RequiredModule = value });
    }

    private Task SetUtcNowAsync(string value)
    {
        return UpdateAsync(Value with { UtcNow = value });
    }

    private Task SetProcessRuntimeAsync(int value)
    {
        return UpdateAsync(Value with { ProcessRuntimeMinutes = Math.Clamp(value, 0, 10_080) });
    }

    private Task SetLicensedMachineAsync(string value)
    {
        return UpdateAsync(Value with { LicensedMachineFingerprint = value });
    }

    private Task SetCurrentMachineAsync(string value)
    {
        return UpdateAsync(Value with { CurrentMachineFingerprint = value });
    }

    private Task SetLicensePskAsync(string value)
    {
        return UpdateAsync(Value with { LicensePresharedKey = value });
    }

    private Task SetSuppliedPskAsync(string value)
    {
        return UpdateAsync(Value with { SuppliedPresharedKey = value });
    }

    private Task UpdateAsync(LicensingInput input)
    {
        return ValueChanged.InvokeAsync(input);
    }
}
