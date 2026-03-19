Param(
  [string]$msbuildPath = "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe",
  [string]$defaultBranch = "develop"
)

# 1 - Define the parent directory where this script and the BHoM_Installer folder reside
$baseDir = $PSScriptRoot
$installerPath = Join-Path -Path $baseDir -ChildPath "IncludedRepos"

# **** READ ARRAYS FROM LOCAL FILES ****
$cores        = Get-Content (Join-Path $installerPath "core.txt")
$adapterCores = Get-Content (Join-Path $installerPath "adapterCore.txt")
$uiCores      = Get-Content (Join-Path $installerPath "uiCore.txt")
$dependencies = Get-Content (Join-Path $installerPath "dependencies.txt")
$includes     = Get-Content (Join-Path $installerPath "include.txt")
$uis          = Get-Content (Join-Path $installerPath "userInterfaces.txt")
$altconfigs   = Get-Content (Join-Path $installerPath "altConfigs.txt")

# **** Git Config ****
git config --global user.email "linhnamnguyen.arc@gmail.com"
git config --global user.name "linh-nam.nguyen"

# Helper functions
function Invoke-Clone {
  param([string]$repo, [string]$branch)
  
  $targetBranch = $defaultBranch
  if ($branch) {
    $targetBranch = $branch 
  }

  & (Join-Path $baseDir "Clone.ps1") -repo $repo -branch $targetBranch
}

function Invoke-Build {
  param([string]$repo, [string]$configuration = "Release")
  # Build.ps1 only needs the "Org/Repo" part
  $cleanRepo = $repo.Split(">")[0].Trim()
  
  & (Join-Path $baseDir "BuildRepos.ps1") -repo $cleanRepo -msbuildPath $msbuildPath -configuration $configuration
}

function Process-RepoLine {
  param([string]$line, [bool]$shouldBuild)
  
  if ([string]::IsNullOrWhiteSpace($line)) {
    return 
  }

  # Split "Org/Repo >Branch"
  $parts = $line.Split(">")
  $repoFull = $parts[0].Trim()
  $branch = $null
  if ($parts.Count -gt 1) {
    $branch = $parts[1].Trim()
  }

  # For standard repos, repoFull is "Org/Repo"
  # For altConfigs, it might be "Org/Repo/Config"
  $repoParts = $repoFull.Split("/")
  $repoPath = "$($repoParts[0])/$($repoParts[1])"
  $config = "Release"
  if ($repoParts.Count -gt 2) {
    $config = $repoParts[2]
  }

  Invoke-Clone -repo $repoPath -branch $branch
  
  if ($shouldBuild) {
    Invoke-Build -repo $repoPath -configuration $config
  }
}

# **** Iterate over Core repos ****
Write-Output ("**** ITERATING OVER CORES ****")
ForEach ($line in $cores) {
  Process-RepoLine -line $line -shouldBuild $true
}

# **** Iterate over Adapter Core repos ****
Write-Output ("**** ITERATING OVER ADAPTER CORES ****")
ForEach ($line in $adapterCores) {
  Process-RepoLine -line $line -shouldBuild $true
}

# **** Iterate over UI Core repos ****
Write-Output ("**** ITERATING OVER UI CORES ****")
ForEach ($line in $uiCores) {
  Process-RepoLine -line $line -shouldBuild $true
}

# **** Iterate over all dependencies ****
Write-Output ("**** ITERATING OVER ALL DEPENDENCIES ****")
ForEach ($line in $dependencies) {
  Process-RepoLine -line $line -shouldBuild $true
}

# **** Iterate over all Toolkits to Include ****
Write-Output ("**** ITERATING OVER ALL TOOLKITS TO INCLUDE ****")
ForEach ($line in $includes) {
  Process-RepoLine -line $line -shouldBuild $true
}

# **** Handle Alternate Configs ****
Write-Output ("**** HANDLING ALTERNATE CONFIGS ****")
ForEach ($line in $altconfigs) {
  Process-RepoLine -line $line -shouldBuild $true
}

# **** Iterate over all UIs ****
Write-Output ("**** ITERATING OVER ALL USER INTERFACES ****")
ForEach ($line in $uis) {
  Process-RepoLine -line $line -shouldBuild $true
}
