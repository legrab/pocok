// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pocok.Conversion;
using Pocok.Showcase.Components;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Conversion.Models;

namespace Pocok.Showcase.Conversion;

public partial class ConversionEditor
{
    private ShowcaseCodeAssistEditor? _codeEditor;
    private string? _formatError;
    private ShowcaseBufferedTextArea? _sourceValueEditor;

    [Parameter][EditorRequired] public ConversionInput Value { get; set; } = new();

    [Parameter] public EventCallback<ConversionInput> ValueChanged { get; set; }

    [Parameter]
    [EditorRequired]
    public ShowcaseCodeAssistCatalog CodeAssist { get; set; } = ShowcaseCodeAssistCatalog.Empty;

    [Parameter][EditorRequired] public IShowcaseText Text { get; set; } = default!;

    private int DisplayedMaximumItems => Value.MaximumCollectionItems == 10_000
        ? 500
        : Value.MaximumCollectionItems;

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

    private static TEnum ReadEnum<TEnum>(ChangeEventArgs args, TEnum fallback)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(ReadString(args), out TEnum value) ? value : fallback;
    }

    private string T(string key)
    {
        return Text.GetText("conversion", key);
    }

    /// <summary>Flushes the active browser-owned editor value before an explicit action.</summary>
    public async Task FlushAsync()
    {
        await FlushActiveInputAsync();
    }

    private async Task<ConversionInput> FlushActiveInputAsync()
    {
        ConversionInput input = Value;
        if (Value.EditorMode == ConversionEditorMode.Fields && _sourceValueEditor is not null)
        {
            var sourceValue = await _sourceValueEditor.FlushAsync();
            return input with { SourceValue = sourceValue };
        }

        if (Value.EditorMode == ConversionEditorMode.Code && _codeEditor is not null)
        {
            var code = await _codeEditor.FlushAsync();
            return input with { Code = code };
        }

        return input;
    }

    private Task SetModeAsync(string mode)
    {
        return SetModeAsync(Enum.Parse<ConversionEditorMode>(mode));
    }

    private async Task SetModeAsync(ConversionEditorMode mode)
    {
        ConversionInput input = await FlushActiveInputAsync();
        if (input.EditorMode == mode)
            return;

        if (mode == ConversionEditorMode.Code)
        {
            _formatError = null;
            await UpdateAsync(input with { EditorMode = mode });
            return;
        }

        ConversionParseResult parsed = ConversionCodeParser.Parse(input.Code, input.SampleId);
        if (parsed.IsSuccess)
        {
            ConversionInput fields = parsed.Input! with
            {
                SampleId = input.SampleId,
                EditorMode = ConversionEditorMode.Fields
            };
            await UpdateAsync(fields);
            return;
        }

        await ValueChanged.InvokeAsync(input with { EditorMode = ConversionEditorMode.Fields });
    }

    private Task SetSourceKindAsync(ConversionSourceKind value)
    {
        return UpdateAsync(Value with
        {
            SourceKind = value,
            SourceValue = value == ConversionSourceKind.Null ? string.Empty : Value.SourceValue
        });
    }

    private Task SetTargetTypeAsync(string value)
    {
        return UpdateAsync(Value with { TargetType = value });
    }

    private Task SetSourceValueAsync(string value)
    {
        return UpdateAsync(Value with { SourceValue = value });
    }

    private Task SetCultureAsync(string value)
    {
        return UpdateAsync(Value with { Culture = value });
    }

    private Task SetOverflowAsync(OverflowPolicy value)
    {
        return UpdateAsync(Value with { Overflow = value });
    }

    private Task SetNullPolicyAsync(NullPolicy value)
    {
        return UpdateAsync(Value with { Nulls = value });
    }

    private Task SetEnumPolicyAsync(EnumPolicy value)
    {
        return UpdateAsync(Value with { Enums = value });
    }

    private Task SetNumericLossAsync(NumericLossPolicy value)
    {
        return UpdateAsync(Value with { NumericLoss = value });
    }

    private Task SetNumericBooleansAsync(NumericBooleanPolicy value)
    {
        return UpdateAsync(Value with { NumericBooleans = value });
    }

    private Task SetTemporalTextAsync(TemporalTextPolicy value)
    {
        return UpdateAsync(Value with { TemporalText = value });
    }

    private Task SetMaximumDepthAsync(int value)
    {
        return UpdateAsync(Value with
        {
            MaximumDepth = Math.Clamp(value, 1, 64)
        });
    }

    private Task SetMaximumItemsAsync(int value)
    {
        return UpdateAsync(Value with
        {
            MaximumCollectionItems = Math.Clamp(value, 1, 500)
        });
    }

    private Task SetCodeAsync(string value)
    {
        _formatError = null;
        return ValueChanged.InvokeAsync(Value with { Code = value });
    }

    private Task FormatCodeAsync(string code)
    {
        ConversionParseResult parsed = ConversionCodeParser.Parse(code, Value.SampleId);
        if (!parsed.IsSuccess)
        {
            _formatError = $"{T("Sandbox.FormatError")} {parsed.Error}";
            return Task.CompletedTask;
        }

        _formatError = null;
        return ValueChanged.InvokeAsync(Value with { Code = ConversionCodeFormatter.Format(parsed.Input!) });
    }

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
