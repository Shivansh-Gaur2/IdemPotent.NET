[CmdletBinding()]
param(
    [string]$Solution = 'IdemShield.sln'
)

$ErrorActionPreference = 'Stop'
$auditOutput = & dotnet list $Solution package --vulnerable --include-transitive --format json --output-version 1
if ($LASTEXITCODE -ne 0) {
    throw 'NuGet vulnerability audit failed to run.'
}

$audit = $auditOutput | ConvertFrom-Json
$findings = @(
    foreach ($project in @($audit.projects)) {
        foreach ($framework in @($project.frameworks)) {
            $packages = @($framework.topLevelPackages) + @($framework.transitivePackages)
            foreach ($package in $packages) {
                if ($null -ne $package -and
                    $null -ne $package.vulnerabilities -and
                    @($package.vulnerabilities).Count -gt 0) {
                    [PSCustomObject]@{
                        Project = $project.path
                        Package = $package.id
                        Version = $package.resolvedVersion
                    }
                }
            }
        }
    }
)

if ($findings.Count -gt 0) {
    $summary = ($findings | ForEach-Object { "$($_.Package) $($_.Version) in $($_.Project)" }) -join [Environment]::NewLine
    throw "NuGet reported known vulnerable packages:$([Environment]::NewLine)$summary"
}

Write-Host "NuGet vulnerability audit passed for $(@($audit.projects).Count) projects."
