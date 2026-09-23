$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (-not (Get-Command java -ErrorAction SilentlyContinue)) { throw 'Java 21 est requis.' }
if (-not (Test-Path 'eula.txt') -or (Get-Content 'eula.txt' -Raw) -notmatch 'eula=true') {
    Write-Host 'Lisez le contrat dans eula.txt et acceptez-le explicitement avant de démarrer le serveur.'
    if (Test-Path 'eula.txt') { Start-Process notepad.exe -ArgumentList 'eula.txt' }
    exit 1
}
if (-not (Test-Path 'fabric-server-launch.jar')) { throw 'Serveur non installé. Exécutez Install-Server.ps1.' }
& java -Xms2G -Xmx6G -jar 'fabric-server-launch.jar' nogui
