# Clone
Param(
    [Parameter(Mandatory=$true)]
    [string]$repo,
    [Parameter(Mandatory=$true)]
    [string]$branch
)

# 1 - Get the parent directory of where the script is located
$baseDir = Split-Path -Path $PSScriptRoot -Parent

# Split "Org/Repo" to get the individual names
$parts = $repo.Split("/")
if ($parts.Count -lt 2) {
    Write-Warning "Invalid repo format: $repo. Expected 'Org/Repo'."
    return
}
$org = $parts[0]
$repoName = $parts[1]

# Set the target path in the parent directory
$targetPath = Join-Path -Path $baseDir -ChildPath $repoName

# **** Cloning Repo ****
# Check if the folder already exists to prevent git errors
if (Test-Path $targetPath) {
    Write-Output "Directory $repoName already exists. Updating to branch $branch..."
    Set-Location $targetPath
    git fetch -q
    git checkout -q $branch
    git pull -q origin $branch
}
else {
    Write-Output "Cloning $repoName ($branch) to $targetPath"
    git clone -q --branch $branch "https://github.com/$org/$repoName.git" $targetPath
}

Write-Output "Successfully updated $repoName to branch $branch."
