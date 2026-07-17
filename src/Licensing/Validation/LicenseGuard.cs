// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;
using Pocok.Licensing.Runtime;

namespace Pocok.Licensing.Validation;

/// <summary>Provides explicit enforcement for license requirement metadata.</summary>
public static class LicenseGuard
{
    /// <summary>Demands every <see cref="RequiresLicenseAttribute" /> attached to a member or its declaring type.</summary>
    /// <param name="licenses">The license service.</param>
    /// <param name="member">The reflected class or method.</param>
    /// <exception cref="LicenseException">A declared module is not licensed.</exception>
    public static void DemandFor(this ILicenseService licenses, MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(licenses);
        ArgumentNullException.ThrowIfNull(member);

        IEnumerable<RequiresLicenseAttribute> requirements = member is Type
            ? member.GetCustomAttributes<RequiresLicenseAttribute>(true)
            : (member.DeclaringType?.GetCustomAttributes<RequiresLicenseAttribute>(true) ?? [])
            .Concat(member.GetCustomAttributes<RequiresLicenseAttribute>(true));

        foreach (var module in requirements
                     .Select(requirement => requirement.Module)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
            licenses.Demand(module);
    }
}
