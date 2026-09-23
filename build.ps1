$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src/CobblemonHarcadiaAcademy.Launcher/CobblemonHarcadiaAcademy.Launcher.csproj'
$publishDir = Join-Path $PSScriptRoot 'outputs/publish'
$outputExe = Join-Path $PSScriptRoot 'outputs/CobblemonHarcadiaAcademy.exe'

$localDotnet = Join-Path $PSScriptRoot 'work/dotnet-sdk/dotnet.exe'
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) { $dotnetExe = $dotnet.Source }
elseif (Test-Path $localDotnet) { $dotnetExe = $localDotnet }
else { throw 'Le SDK .NET 9 est requis. Installez-le depuis https://dotnet.microsoft.com/download/dotnet/9.0 puis relancez build.ps1.' }

& $dotnetExe publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:NuGetAudit=false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish a échoué ($LASTEXITCODE)." }
Copy-Item -LiteralPath (Join-Path $publishDir 'CobblemonHarcadiaAcademy.exe') -Destination $outputExe -Force
Write-Host "EXE prêt : $outputExe"
