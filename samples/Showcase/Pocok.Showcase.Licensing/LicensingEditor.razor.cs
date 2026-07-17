// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Licensing.Models;

namespace Pocok.Showcase.Licensing;

public partial class LicensingEditor
{
    [Parameter, EditorRequired]
    public LicensingInput Value { get; set; } = new();

    [Parameter]
    public EventCallback<LicensingInput> ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public IShowcaseText Text { get; set; } = default!;

    private static string ReadString(ChangeEventArgs args) => args.Value?.ToString() ?? string.Empty;

    private static T ReadValue<T>(ChangeEventArgs args, T fallback)
    {
        return BindConverter.TryConvertTo(args.Value, CultureInfo.InvariantCulture, out T? value) && value is not null
            ? value
            : fallback;
    }

    private string T(string key) => Text.GetText("licensing", key);

    private Task SetLicenseIdAsync(string value) => UpdateAsync(Value with { LicenseId = value });
    private Task SetCustomerAsync(string value) => UpdateAsync(Value with { Customer = value });
    private Task SetIssuedAtAsync(string value) => UpdateAsync(Value with { IssuedAtUtc = value });
    private Task SetValidFromAsync(string value) => UpdateAsync(Value with { ValidFromUtc = value });
    private Task SetValidUntilAsync(string value) => UpdateAsync(Value with { ValidUntilUtc = value });
    private Task SetMaximumRuntimeAsync(int value) => UpdateAsync(Value with { MaximumProcessRuntimeMinutes = Math.Clamp(value, 0, 10_080) });
    private Task SetAllModulesAsync(bool value) => UpdateAsync(Value with { AllModules = value });
    private Task SetLicensedModulesAsync(string value) => UpdateAsync(Value with { LicensedModules = value });
    private Task SetRequiredModuleAsync(string value) => UpdateAsync(Value with { RequiredModule = value });
    private Task SetUtcNowAsync(string value) => UpdateAsync(Value with { UtcNow = value });
    private Task SetProcessRuntimeAsync(int value) => UpdateAsync(Value with { ProcessRuntimeMinutes = Math.Clamp(value, 0, 10_080) });
    private Task SetLicensedMachineAsync(string value) => UpdateAsync(Value with { LicensedMachineFingerprint = value });
    private Task SetCurrentMachineAsync(string value) => UpdateAsync(Value with { CurrentMachineFingerprint = value });
    private Task SetLicensePskAsync(string value) => UpdateAsync(Value with { LicensePresharedKey = value });
    private Task SetSuppliedPskAsync(string value) => UpdateAsync(Value with { SuppliedPresharedKey = value });

    private Task UpdateAsync(LicensingInput input) => ValueChanged.InvokeAsync(input);
}
