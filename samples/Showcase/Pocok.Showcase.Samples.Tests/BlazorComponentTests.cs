// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Pocok.Showcase.Components;
using Pocok.Showcase.Conversion;
using Pocok.Showcase.Conversion.Models;
using Pocok.Showcase.Licensing;
using Pocok.Showcase.Licensing.Models;
using Pocok.Showcase.Scripting;
using Pocok.Showcase.Scripting.Models;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture]
public sealed class BlazorComponentTests
{
    [TestCase(typeof(ConversionEditor), typeof(ConversionInput))]
    [TestCase(typeof(ScriptingEditor), typeof(ScriptingInput))]
    [TestCase(typeof(LicensingEditor), typeof(LicensingInput))]
    public void EditorsExposeTheStandardTwoWayBindingContract(Type componentType, Type valueType)
    {
        typeof(IComponent).IsAssignableFrom(componentType).ShouldBeTrue();

        PropertyInfo value = componentType.GetProperty("Value")!;
        PropertyInfo valueChanged = componentType.GetProperty("ValueChanged")!;

        value.PropertyType.ShouldBe(valueType);
        valueChanged.PropertyType.ShouldBe(typeof(EventCallback<>).MakeGenericType(valueType));
    }

    [TestCase(typeof(ConversionPage))]
    [TestCase(typeof(ScriptingPage))]
    [TestCase(typeof(LicensingPage))]
    public void PluginPagesRemainBlazorComponents(Type pageType) =>
        typeof(IComponent).IsAssignableFrom(pageType).ShouldBeTrue();

    [Test]
    public void SharedModeSwitchExposesAStringSelectionContract()
    {
        typeof(IComponent).IsAssignableFrom(typeof(ShowcaseModeSwitch)).ShouldBeTrue();
        typeof(ShowcaseModeSwitch).GetProperty("SelectedValue")!.PropertyType.ShouldBe(typeof(string));
        typeof(ShowcaseModeSwitch).GetProperty("SelectedValueChanged")!.PropertyType.ShouldBe(typeof(EventCallback<string>));
    }
}
