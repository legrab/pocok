// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Pocok.Licensing.Runtime;

/// <summary>Hashes a platform machine identifier together with OS and architecture context.</summary>
/// <remarks>
///     Windows uses MachineGuid, Linux uses machine-id, and other platforms fall back to the machine name.
///     Set <c>POCOK_MACHINE_ID</c> to provide a deterministic container or platform-specific identifier.
/// </remarks>
public sealed class DefaultMachineFingerprintProvider : IMachineFingerprintProvider
{
    /// <inheritdoc />
    public string GetFingerprint()
    {
        var overridden = Environment.GetEnvironmentVariable("POCOK_MACHINE_ID");
        var machineId = string.IsNullOrWhiteSpace(overridden) ? ReadPlatformMachineId() : overridden;
        var material = string.Join(
            '|',
            "pocok-machine/v1",
            machineId.Trim(),
            Environment.OSVersion.Platform,
            RuntimeInformation.OSArchitecture);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private static string ReadPlatformMachineId()
    {
        try
        {
            if (OperatingSystem.IsWindows())
                foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                    using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                    if (key?.GetValue("MachineGuid") is string machineGuid && !string.IsNullOrWhiteSpace(machineGuid))
                        return machineGuid;
                }

            if (OperatingSystem.IsLinux())
                foreach (var path in new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" })
                    if (File.Exists(path))
                        return File.ReadAllText(path).Trim();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            // The documented machine-name fallback keeps validation available on restricted hosts.
        }

        return Environment.MachineName;
    }
}
