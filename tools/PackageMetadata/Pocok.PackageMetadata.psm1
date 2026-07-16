Set-StrictMode -Version Latest

function Get-PocokNuspecMetadata {
    [CmdletBinding()]
    param([Parameter(Mandatory)][xml]$Nuspec)

    $metadata = $Nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    if ($null -eq $metadata) { throw 'The nuspec has no metadata element.' }

    $dependencies = @(
        $Nuspec.SelectNodes(
            "/*[local-name()='package']" +
            "/*[local-name()='metadata']" +
            "/*[local-name()='dependencies']" +
            "//*[local-name()='dependency']"
        )
    ) | ForEach-Object {
        [pscustomobject]@{
            Id = [string]$_.id
            Version = [string]$_.version
            TargetFramework = if ($_.ParentNode.LocalName -eq 'group') {
                [string]$_.ParentNode.GetAttribute('targetFramework')
            }
            else {
                ''
            }
        }
    } | Sort-Object Id, TargetFramework, Version

    [pscustomobject]@{
        Id = [string]$metadata.id
        Version = [string]$metadata.version
        MetadataNode = $metadata
        Dependencies = @($dependencies)
    }
}

Export-ModuleMember -Function Get-PocokNuspecMetadata
