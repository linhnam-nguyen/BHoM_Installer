Param(
    [Parameter(Mandatory=$true)]
    [string]$repo,
    [Parameter(Mandatory=$true)]
    [string]$msbuildPath,
    [string]$configuration = "Release"
)

# 1 - Get the parent directory of where the script is located (where repos are cloned)
$baseDir = Split-Path -Path $PSScriptRoot -Parent

# Split "Org/Repo" to get the individual names
$parts = $repo.Split("/")
if ($parts.Count -lt 2) {
    Write-Warning "Invalid repo format: $repo. Expected 'Org/Repo'."
    return
}
$repoName = $parts[1]

# Set the target path in the parent directory
$targetPath = Join-Path -Path $baseDir -ChildPath $repoName
$slnPath = Join-Path -Path $targetPath -ChildPath "$repoName.sln"

if (-not (Test-Path $slnPath)) {
    Write-Warning "Solution file not found at $slnPath. Skipping build for $repoName."
    return
}

# **** Restore NuGet ****
Write-Output ("Restoring NuGet packages for " + $repoName + " (using MSBuild)")
# Using the built-in restore target in MSBuild
& $msbuildPath $slnPath /t:restore /p:Configuration=$configuration /nologo /verbosity:minimal

# **** Building .sln ****
Write-Output ("Building " + $repoName + ".sln (" + $configuration + ")")
& $msbuildPath $slnPath /p:Configuration=$configuration /nologo /verbosity:minimal
