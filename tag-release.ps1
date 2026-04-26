# Usage: .\tag-release.ps1 1.2.0
# Updates version in .csproj, commits, creates git tag, and optionally pushes.
param(
    [Parameter(Mandatory)][string]$Version,
    [switch]$Push
)

# Normalize: accept "1.2" -> "1.2.0"
$parts = $Version.TrimStart('v') -split '\.'
while ($parts.Count -lt 3) { $parts += '0' }
$semver = $parts[0..2] -join '.'
$tag    = "v$semver"

$csproj = Join-Path $PSScriptRoot 'WinWidgetTime.csproj'
if (-not (Test-Path $csproj)) {
    Write-Error "WinWidgetTime.csproj not found at $csproj"; exit 1
}

# Check for uncommitted changes (other than the csproj we're about to modify)
$dirty = git status --porcelain | Where-Object { $_ -notmatch 'WinWidgetTime\.csproj' }
if ($dirty) {
    Write-Error "Working tree has uncommitted changes. Commit or stash them first.`n$($dirty -join "`n")"
    exit 1
}

# Check tag doesn't already exist
if (git tag --list $tag) {
    Write-Error "Tag $tag already exists."; exit 1
}

# Update version fields in csproj
$xml = [xml](Get-Content $csproj -Raw)
$pg  = $xml.Project.PropertyGroup | Where-Object { $_.Version -ne $null }
if (-not $pg) {
    Write-Error "<Version> element not found in $csproj"; exit 1
}
$pg.Version         = $semver
$pg.AssemblyVersion = $semver
$pg.FileVersion     = $semver
$xml.Save($csproj)

Write-Host "Updated $csproj -> $semver"

git add WinWidgetTime.csproj
git commit -m "Bump version to $tag"
git tag $tag

Write-Host ""
Write-Host "Created tag $tag. To publish the release run:"
Write-Host "  git push && git push origin $tag"

if ($Push) {
    git push
    git push origin $tag
    Write-Host "Pushed."
}
