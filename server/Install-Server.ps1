$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$mcVersion = '1.21.1'
$modsDir = Join-Path $root 'mods'
$manifestDir = Join-Path $root 'modpack'
$siteManifestDir = Join-Path $root '..\site\modpack'
New-Item -ItemType Directory -Force -Path $modsDir,$manifestDir,$siteManifestDir | Out-Null

$java = Get-Command java -ErrorAction SilentlyContinue
if (-not $java) { throw 'Java 21 est requis. Installe Java 21 x64 puis relancez ce script.' }
$javaVersion = (& java -version 2>&1 | Out-String)
if ($javaVersion -notmatch 'version "(21|22|23|24|25)') { Write-Warning "Java 21 ou plus récent est recommandé. Version trouvée : $javaVersion" }

$installerInfo = Invoke-RestMethod 'https://meta.fabricmc.net/v2/versions/installer'
$installer = $installerInfo | Where-Object stable | Select-Object -First 1
if (-not $installer) { $installer = $installerInfo | Select-Object -First 1 }
$fabricInstaller = Join-Path $root 'fabric-installer.jar'
Invoke-WebRequest "https://maven.fabricmc.net/net/fabricmc/fabric-installer/$($installer.version)/fabric-installer-$($installer.version).jar" -OutFile $fabricInstaller
& java -jar $fabricInstaller server -mcversion $mcVersion -loader latest -downloadMinecraft -dir $root
if ($LASTEXITCODE -ne 0) { throw "L'installation Fabric a échoué ($LASTEXITCODE)." }

$manifest = [ordered]@{ version = '0.1.0'; minecraft = $mcVersion; loader = 'fabric'; files = @() }
$pinnedMods = @(
    @{ slug = 'cobblemon'; versionId = 'YgmyyFcs' },
    @{ slug = 'fabric-api'; versionId = 'IpaMcBLh' }
)
foreach ($pinned in $pinnedMods) {
    $slug = $pinned.slug
    $version = Invoke-RestMethod "https://api.modrinth.com/v2/version/$($pinned.versionId)"
    if ($version.game_versions -notcontains $mcVersion -or $version.loaders -notcontains 'fabric' -or $version.version_type -ne 'release') {
        throw "La version $($version.version_number) de $slug n'est pas une release Fabric vérifiée pour Minecraft $mcVersion."
    }
    $file = $version.files | Where-Object primary | Select-Object -First 1
    if (-not $file) { $file = $version.files | Select-Object -First 1 }
    $destination = Join-Path $modsDir $file.filename
    Invoke-WebRequest $file.url -OutFile $destination
    $hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
    $manifest.files += [ordered]@{ path = "mods/$($file.filename)"; url = $file.url; sha256 = $hash; project = $slug; version = $version.version_number }
    Write-Host "Installé : $slug $($version.version_number)"
}
$manifestJson = $manifest | ConvertTo-Json -Depth 8
$manifestJson | Set-Content -LiteralPath (Join-Path $manifestDir 'mods.json') -Encoding utf8
$manifestJson | Set-Content -LiteralPath (Join-Path $siteManifestDir 'mods.json') -Encoding utf8
Write-Host 'Serveur Fabric/Cobblemon préparé. Consultez eula.txt puis Start-Server.ps1.'
