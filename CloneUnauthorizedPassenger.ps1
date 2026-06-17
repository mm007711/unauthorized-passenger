[CmdletBinding()]
param(
    [string]$TargetDir = (Join-Path (Join-Path $env:USERPROFILE "UnityProjects") "unauthorized-passenger"),
    [switch]$UseZip
)

$ErrorActionPreference = "Stop"

$RepoUrl = "https://github.com/mm007711/unauthorized-passenger.git"
$ZipUrl = "https://github.com/mm007711/unauthorized-passenger/archive/refs/heads/main.zip"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Test-DirectoryEmpty {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $true
    }

    $firstItem = Get-ChildItem -LiteralPath $Path -Force -ErrorAction Stop | Select-Object -First 1
    return $null -eq $firstItem
}

function Ensure-ParentDirectory {
    param([string]$Path)

    $parent = Split-Path -Parent $Path
    if ([string]::IsNullOrWhiteSpace($parent)) {
        throw "Target path has no parent directory: $Path"
    }

    New-Item -ItemType Directory -Force -Path $parent | Out-Null
}

function Get-FullPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path)
}

function Remove-TempDirectory {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        return
    }

    $resolvedTemp = (Resolve-Path -LiteralPath $Path).Path
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd("\")
    $leaf = Split-Path -Leaf $resolvedTemp

    if ($resolvedTemp -ne $systemTemp -and
        $leaf -like "unauthorized-passenger-*" -and
        $resolvedTemp.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}

function Download-ZipProject {
    param([string]$Destination)

    Ensure-ParentDirectory -Path $Destination

    if (-not (Test-Path -LiteralPath $Destination)) {
        New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    }

    if (-not (Test-DirectoryEmpty -Path $Destination)) {
        throw "Target directory already exists and is not empty: $Destination"
    }

    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("unauthorized-passenger-" + [System.Guid]::NewGuid().ToString("N"))
    $zipPath = Join-Path $tempRoot "project.zip"
    $extractRoot = Join-Path $tempRoot "extract"

    try {
        New-Item -ItemType Directory -Force -Path $extractRoot | Out-Null

        Write-Step "Downloading project ZIP from GitHub"
        Invoke-WebRequest -Uri $ZipUrl -OutFile $zipPath

        Write-Step "Extracting project"
        Expand-Archive -LiteralPath $zipPath -DestinationPath $extractRoot -Force

        $sourceDir = Get-ChildItem -LiteralPath $extractRoot -Directory -Force |
            Where-Object { $_.Name -like "unauthorized-passenger-*" } |
            Select-Object -First 1

        if ($null -eq $sourceDir) {
            throw "Could not find extracted project directory."
        }

        Get-ChildItem -LiteralPath $sourceDir.FullName -Force | ForEach-Object {
            Move-Item -LiteralPath $_.FullName -Destination $Destination
        }
    }
    finally {
        Remove-TempDirectory -Path $tempRoot
    }
}

$TargetDir = Get-FullPath -Path $TargetDir
$gitCommand = Get-Command git -ErrorAction SilentlyContinue
$hasGitRepository = Test-Path -LiteralPath (Join-Path $TargetDir ".git")

Write-Host "Unauthorized Passenger project downloader" -ForegroundColor Green
Write-Host "Repository: $RepoUrl"
Write-Host "Target:     $TargetDir"

if ($hasGitRepository -and -not $UseZip) {
    if ($null -eq $gitCommand) {
        throw "The target folder is a Git repository, but Git is not installed or not in PATH."
    }

    Write-Step "Existing project found, pulling latest changes"
    git -C $TargetDir pull --ff-only
}
elseif (-not $UseZip -and $null -ne $gitCommand) {
    if (Test-Path -LiteralPath $TargetDir -and -not (Test-DirectoryEmpty -Path $TargetDir)) {
        throw "Target directory already exists and is not empty: $TargetDir"
    }

    Ensure-ParentDirectory -Path $TargetDir

    Write-Step "Cloning project with Git"
    git clone --recurse-submodules $RepoUrl $TargetDir
}
else {
    Write-Step "Git is unavailable or ZIP mode was requested"
    Download-ZipProject -Destination $TargetDir
}

Write-Step "Done"
Write-Host "Project folder: $TargetDir"
Write-Host "Open Unity Hub, choose Add project from disk, then select this folder."

if (Test-Path -LiteralPath $TargetDir) {
    Start-Process explorer.exe $TargetDir
}
