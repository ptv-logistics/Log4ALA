$NuGetExe = Join-Path $PSScriptRoot '.nuget\nuget.exe'

# Download NuGet.exe if missing
if (-not (Test-Path $NuGetExe)) {
    Write-Host 'Downloading nuget.exe'
    New-Item  -ItemType directory  (Join-Path $PSScriptRoot '.nuget') | Out-Null
    wget https://dist.nuget.org/win-x86-commandline/v4.1.0/nuget.exe -OutFile $NuGetExe
}