// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pocok.Conversion;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Conversion.Models;

namespace Pocok.Showcase.Conversion;

public partial class ConversionEditor
{
    [Parameter, EditorRequired]
    public ConversionInput Value { get; set; } = new();

    [Parameter]
    public EventCallback<ConversionInput> ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public ShowcaseCodeAssistCatalog CodeAssist { get; set; } = ShowcaseCodeAssistCatalog.Empty;

    [Parameter, EditorRequired]
    public IShowcaseText Text { get; set; } = default!;

    private int DisplayedMaximumItems => Value.MaximumCollectionItems == 10_000
        ? 500
        : Value.MaximumCollectionItems;

    private static string ReadString(ChangeEventArgs args) => args.Value?.ToString() ?? string.Empty;

    private static T ReadValue<T>(ChangeEventArgs args, T fallback)
    {
        return BindConverter.TryConvertTo(args.Value, CultureInfo.InvariantCulture, out T? value) && value is not null
            ? value
            : fallback;
    }

    private static TEnum ReadEnum<TEnum>(ChangeEventArgs args, TEnum fallback)
        where TEnum : struct, Enum =>
        Enum.TryParse(ReadString(args), out TEnum value) ? value : fallback;

    private string T(string key) => Text.GetText("conversion", key);

    private Task SetModeAsync(string mode) => SetModeAsync(Enum.Parse<ConversionEditorMode>(mode));

    private async Task SetModeAsync(ConversionEditorMode mode)
    {
        if (Value.EditorMode == mode)
            return;

        if (mode == ConversionEditorMode.Code)
        {
            await UpdateAsync(Value with { EditorMode = mode });
            return;
        }

        ConversionParseResult parsed = ConversionCodeParser.Parse(Value.Code, Value.SampleId);
        if (parsed.IsSuccess)
        {
            ConversionInput fields = parsed.Input! with
            {
                SampleId = Value.SampleId,
                EditorMode = ConversionEditorMode.Fields
            };
            await UpdateAsync(fields);
            return;
        }

        await ValueChanged.InvokeAsync(Value with { EditorMode = ConversionEditorMode.Fields });
    }

    private Task SetSourceKindAsync(ConversionSourceKind value) => UpdateAsync(Value with
    {
        SourceKind = value,
        SourceValue = value == ConversionSourceKind.Null ? string.Empty : Value.SourceValue
    });

    private Task SetTargetTypeAsync(string value) => UpdateAsync(Value with { TargetType = value });

    private Task SetSourceValueAsync(string value) => UpdateAsync(Value with { SourceValue = value });

    private Task SetCultureAsync(string value) => UpdateAsync(Value with { Culture = value });

    private Task SetOverflowAsync(OverflowPolicy value) => UpdateAsync(Value with { Overflow = value });

    private Task SetNullPolicyAsync(NullPolicy value) => UpdateAsync(Value with { Nulls = value });

    private Task SetEnumPolicyAsync(EnumPolicy value) => UpdateAsync(Value with { Enums = value });

    private Task SetNumericLossAsync(NumericLossPolicy value) => UpdateAsync(Value with { NumericLoss = value });

    private Task SetNumericBooleansAsync(NumericBooleanPolicy value) => UpdateAsync(Value with { NumericBooleans = value });

    private Task SetTemporalTextAsync(TemporalTextPolicy value) => UpdateAsync(Value with { TemporalText = value });

    private Task SetMaximumDepthAsync(int value) => UpdateAsync(Value with
    {
        MaximumDepth = Math.Clamp(value, 1, 64)
    });

    private Task SetMaximumItemsAsync(int value) => UpdateAsync(Value with
    {
        MaximumCollectionItems = Math.Clamp(value, 1, 500)
    });

    private Task SetCodeAsync(string value) => ValueChanged.InvokeAsync(Value with { Code = value });

    private Task UpdateAsync(ConversionInput input)
    {
        try
        {
            input = input with { Code = ConversionCodeFormatter.Format(input) };
        }
        catch (FormatException)
        {
            input = input with { Code = Value.Code };
        }

        return ValueChanged.InvokeAsync(input);
    }
}
