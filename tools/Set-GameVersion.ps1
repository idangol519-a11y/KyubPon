<#
.SYNOPSIS
    Sets or checks the KyubPon game version in every place it is written down.

.DESCRIPTION
    The game version lives in four files. This script keeps them identical:

      1. ProjectSettings/ProjectSettings.asset  (Unity: Project Settings > Player > Version).
         This is the real source: the game reads it through Application.version
         and shows it on the Home screen and in the log.
      2. docs/GameState.md                      ("Current version")
      3. README.md                              ("version `x.y.z`")
      4. CHANGELOG.md                           (a "## [x.y.z] - date" section)

    Versions follow Semantic Versioning: MAJOR.MINOR.PATCH, for example 0.3.0,
    with an optional pre-release tag such as 0.3.0-alpha.

.EXAMPLE
    .\tools\Set-GameVersion.ps1 -Check
    Shows the version found in each place and fails if they differ.

.EXAMPLE
    .\tools\Set-GameVersion.ps1 -Version 0.3.0
    Writes 0.3.0 to all four files. Git commit and tag are done separately;
    see docs/Releasing.md.
#>
param(
    [string]$Version,
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$SemanticVersionPattern = '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$'
$AnyVersion = '\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?'

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PlayerSettingsFile = Join-Path $ProjectRoot 'ProjectSettings\ProjectSettings.asset'
$GameStateFile = Join-Path $ProjectRoot 'docs\GameState.md'
$ReadmeFile = Join-Path $ProjectRoot 'README.md'
$ChangelogFile = Join-Path $ProjectRoot 'CHANGELOG.md'

# Each entry: the file, and a pattern whose first group is the text before the
# version and whose second group is the version itself.
$VersionPlaces = [ordered]@{
    'Unity Player Settings' = @{ File = $PlayerSettingsFile; Pattern = "(?m)(^  bundleVersion: )($AnyVersion)" }
    'docs/GameState.md'     = @{ File = $GameStateFile;      Pattern = "(Current version: \*\*)($AnyVersion)" }
    'README.md'             = @{ File = $ReadmeFile;         Pattern = "(version ``)($AnyVersion)" }
}

function Read-Text([string]$Path) {
    return [System.IO.File]::ReadAllText($Path)
}

function Write-Text([string]$Path, [string]$Text) {
    # UTF-8 without a byte-order mark, which is what Unity and Git expect.
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding $false))
}

function Get-VersionInFile([string]$Path, [string]$Pattern) {
    $match = [regex]::Match((Read-Text $Path), $Pattern)
    if ($match.Success) { return $match.Groups[2].Value }
    return $null
}

function Get-LatestChangelogVersion {
    $match = [regex]::Match((Read-Text $ChangelogFile), "(?m)^## \[($AnyVersion)\]")
    if ($match.Success) { return $match.Groups[1].Value }
    return $null
}

function Get-LatestGitTag {
    $tag = git -C $ProjectRoot tag --list 'v*' --sort=-v:refname | Select-Object -First 1
    if ($tag) { return $tag.TrimStart('v') }
    return $null
}

function Show-Versions {
    $found = [ordered]@{}
    foreach ($name in $VersionPlaces.Keys) {
        $place = $VersionPlaces[$name]
        $found[$name] = Get-VersionInFile $place.File $place.Pattern
    }
    $found['CHANGELOG.md (newest section)'] = Get-LatestChangelogVersion

    foreach ($name in $found.Keys) {
        $value = $found[$name]
        if (-not $value) { $value = '(not found)' }
        Write-Host ('  {0,-32} {1}' -f $name, $value)
    }

    $tag = Get-LatestGitTag
    $tagText = '(no tags yet)'
    if ($tag) { $tagText = "v$tag" }
    Write-Host ('  {0,-32} {1}' -f 'Newest git tag', $tagText)

    $distinct = @($found.Values | Sort-Object -Unique)
    if ($distinct.Count -ne 1 -or -not $distinct[0]) {
        Write-Host ''
        Write-Host 'MISMATCH: the files above do not agree. Run this script with -Version to fix them.' -ForegroundColor Red
        return $false
    }

    Write-Host ''
    Write-Host "OK: every file says $($distinct[0])." -ForegroundColor Green
    if ($tag -ne $distinct[0]) {
        Write-Host "Note: this version has no git tag yet. Tag it as v$($distinct[0]) when it is merged to main." -ForegroundColor Yellow
    }
    return $true
}

function Set-VersionInFile([string]$Name, [string]$Path, [string]$Pattern, [string]$NewVersion) {
    $text = Read-Text $Path
    if (-not [regex]::IsMatch($text, $Pattern)) {
        throw "Could not find the version in $Name ($Path). Nothing was changed in that file."
    }
    $updated = ([regex]$Pattern).Replace($text, { param($m) $m.Groups[1].Value + $NewVersion }, 1)
    Write-Text $Path $updated
}

function Add-ChangelogSection([string]$NewVersion) {
    $text = Read-Text $ChangelogFile
    if ($text -match "(?m)^## \[$([regex]::Escape($NewVersion))\]") {
        return
    }
    if ($text -notmatch '(?m)^## \[Unreleased\]') {
        throw "CHANGELOG.md has no '## [Unreleased]' heading, so the new section could not be added."
    }

    # Everything listed under Unreleased becomes the notes of the new version.
    $newline = "`n"
    if ($text.Contains("`r`n")) { $newline = "`r`n" }
    $today = Get-Date -Format 'yyyy-MM-dd'
    $heading = "## [Unreleased]$newline$newline## [$NewVersion] - $today"
    $updated = ([regex]'(?m)^## \[Unreleased\]').Replace($text, $heading, 1)
    Write-Text $ChangelogFile $updated
}

if ($Check -or -not $Version) {
    Write-Host 'KyubPon version check'
    if (Show-Versions) { exit 0 }
    exit 1
}

if ($Version -notmatch $SemanticVersionPattern) {
    throw "'$Version' is not a valid version. Use MAJOR.MINOR.PATCH, for example 0.3.0 or 0.3.0-alpha."
}

foreach ($name in $VersionPlaces.Keys) {
    $place = $VersionPlaces[$name]
    Set-VersionInFile $name $place.File $place.Pattern $Version
}
Add-ChangelogSection $Version

Write-Host "KyubPon version set to $Version"
if (-not (Show-Versions)) { exit 1 }
Write-Host ''
Write-Host 'Next: check the notes in CHANGELOG.md, then commit, merge, and tag. See docs/Releasing.md.'
exit 0
