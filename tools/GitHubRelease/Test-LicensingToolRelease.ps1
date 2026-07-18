# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$resolver = Join-Path $PSScriptRoot 'Resolve-LicensingToolFromTag.ps1'
$keygen = & $resolver -Tag 'licensing.keygen-v1.2.3-alpha.1'
$checker = & $resolver -Tag 'licensing.licensechecker-v2.0.0'

if ($keygen.'tool-id' -ne 'Pocok.Licensing.Keygen' -or $keygen.version -ne '1.2.3-alpha.1') {
    throw 'Keygen release tag resolution failed.'
}
if ($checker.'tool-id' -ne 'Pocok.Licensing.LicenseChecker' -or $checker.version -ne '2.0.0') {
    throw 'LicenseChecker release tag resolution failed.'
}

foreach ($invalidTag in @('licensing.keygen-v1.2', 'licensing.unknown-v1.0.0')) {
    try {
        & $resolver -Tag $invalidTag | Out-Null
        throw "Invalid tag '$invalidTag' was accepted."
    }
    catch {
        if ($_.Exception.Message -eq "Invalid tag '$invalidTag' was accepted.") {
            throw
        }
    }
}

Write-Host 'Licensing tool release resolution passed.'
