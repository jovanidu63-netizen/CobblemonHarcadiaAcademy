using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ProcessBuilder;
using Microsoft.Win32;
using XboxAuthNet.Game;

namespace HarcadiaLauncher;

public partial class MainWindow : Window
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(10) };
    private readonly string _configFile = Path.Combine(AppContext.BaseDirectory, "launcher.config.json");
    private readonly string _userSettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CobblemonHarcadiaAcademy", "settings.json");
    private LauncherConfig _config = new();
    private JELoginHandler? _loginHandler;
    private MSession? _session;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        try
        {
            if (File.Exists(_configFile)) _config = JsonSerializer.Deserialize<LauncherConfig>(File.ReadAllText(_configFile), JsonOptions()) ?? new();
            _loginHandler = JELoginHandlerBuilder.BuildDefault();
            Title = _config.ServerName;
            TitleText.Text = _config.ServerName;
            VersionText.Text = $"MINECRAFT {_config.MinecraftVersion} · FABRIC";
            ServerText.Text = IsPlaceholder(_config.ServerAddress) ? "Serveur : adresse à configurer" : $"Serveur : {_config.ServerAddress}:{_config.ServerPort}";
            RamSlider.Minimum = Math.Clamp(_config.MinimumRamMb / 1024, 2, 16);
            RamSlider.Maximum = Math.Clamp(_config.MaximumRamMb / 1024, 4, 32);
            RamSlider.Value = Math.Clamp(_config.DefaultRamMb / 1024, (int)RamSlider.Minimum, (int)RamSlider.Maximum);
            var defaultDir = string.IsNullOrWhiteSpace(_config.GameDirectory)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _config.ServerName, ".minecraft")
                : Environment.ExpandEnvironmentVariables(_config.GameDirectory);
            DirectoryBox.Text = defaultDir;
            if (File.Exists(_userSettingsFile))
            {
                var saved = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_userSettingsFile), JsonOptions());
                if (saved is not null)
                {
                    if (!string.IsNullOrWhiteSpace(saved.GameDirectory)) DirectoryBox.Text = saved.GameDirectory;
                    if (saved.RamMb > 0) RamSlider.Value = Math.Clamp(saved.RamMb / 1024d, RamSlider.Minimum, RamSlider.Maximum);
                }
            }
        }
        catch (Exception ex) { StatusText.Text = $"Erreur de configuration : {ex.Message}"; }
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _loginHandler is null) return;
        LoginButton.IsEnabled = false;
        LoginButton.Content = "CONNEXION…";
        StatusText.Text = "Connexion sécurisée à Microsoft…";
        try
        {
            _session = _session is null
                ? await _loginHandler.Authenticate()
                : await _loginHandler.AuthenticateInteractively();
            UsernameBox.Text = _session.Username;
            PlayButton.IsEnabled = true;
            LoginButton.Content = "CHANGER DE COMPTE MICROSOFT";
            StatusText.Text = $"Compte {_session.Username} connecté. Prêt à installer le jeu.";
        }
        catch (Exception ex)
        {
            _session = null;
            PlayButton.IsEnabled = false;
            UsernameBox.Text = "Non connecté";
            LoginButton.Content = "SE CONNECTER AVEC MICROSOFT";
            StatusText.Text = "La connexion Microsoft n’a pas abouti.";
            MessageBox.Show(ex.Message, "Connexion Microsoft", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { LoginButton.IsEnabled = true; }
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (_session is null)
        { MessageBox.Show("Connecte-toi avec un compte Microsoft qui possède Minecraft Java.", _config.ServerName); return; }
        if (IsPlaceholder(_config.ModpackManifestUrl))
        {
            var result = MessageBox.Show("L’URL du manifeste modpack n’est pas configurée. Installer Minecraft/Fabric sans le modpack ?", _config.ServerName, MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes) return;
        }
        SetBusy(true);
        try
        {
            var root = DirectoryBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("Choisis un dossier de jeu.");
            Directory.CreateDirectory(root);
            SaveSettings();
            var mcPath = new MinecraftPath(root);
            var launcher = new MinecraftLauncher(mcPath);
            var fabric = new FabricInstaller(_http);
            string loaderVersion;
            if (string.Equals(_config.FabricLoaderVersion, "latest", StringComparison.OrdinalIgnoreCase))
                loaderVersion = (await fabric.GetFirstLoader(_config.MinecraftVersion))?.Version ?? throw new InvalidOperationException("Aucune version Fabric compatible n’a été trouvée.");
            else loaderVersion = _config.FabricLoaderVersion;

            StatusText.Text = "Installation de Fabric…";
            var versionName = await fabric.Install(_config.MinecraftVersion, loaderVersion, mcPath);
            StatusText.Text = "Téléchargement des fichiers Minecraft, Java et ressources…";
            Progress.IsIndeterminate = true;
            await launcher.InstallAsync(versionName);
            Progress.IsIndeterminate = false;
            if (!IsPlaceholder(_config.ModpackManifestUrl)) await SyncModpackAsync(root, _config.ModpackManifestUrl);

            StatusText.Text = "Préparation du jeu…";
            var options = new MLaunchOption
            {
                Session = _session,
                MaximumRamMb = (int)RamSlider.Value * 1024,
                MinimumRamMb = Math.Min(_config.MinimumRamMb, (int)RamSlider.Value * 1024)
            };
            if (!IsPlaceholder(_config.ServerAddress))
            {
                options.ServerIp = _config.ServerAddress;
                options.ServerPort = _config.ServerPort;
            }
            var process = await launcher.BuildProcessAsync(versionName, options);
            process.Start();
            StatusText.Text = IsPlaceholder(_config.ServerAddress)
                ? "Minecraft est lancé. L’adresse du serveur sera activée après sa mise en place."
                : $"Minecraft est lancé. Connexion à {_config.ServerAddress}…";
        }
        catch (Exception ex)
        {
            Progress.IsIndeterminate = false;
            StatusText.Text = "L’installation ou le lancement a échoué.";
            MessageBox.Show(ex.Message, "Erreur du launcher", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { SetBusy(false); }
    }

    private async Task SyncModpackAsync(string gameRoot, string manifestUrl)
    {
        if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("modpackManifestUrl doit être une URL HTTPS valide.");
        StatusText.Text = "Récupération de la liste des fichiers du modpack…";
        var json = await _http.GetStringAsync(uri);
        var manifest = JsonSerializer.Deserialize<ModpackManifest>(json, JsonOptions()) ?? throw new InvalidOperationException("Le manifeste modpack est invalide.");
        if (!string.IsNullOrWhiteSpace(manifest.Minecraft) && !manifest.Minecraft.Equals(_config.MinecraftVersion, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Le modpack est prévu pour Minecraft {manifest.Minecraft}, pas {_config.MinecraftVersion}.");
        if (!string.IsNullOrWhiteSpace(manifest.Loader) && !manifest.Loader.Equals("fabric", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ce modpack n'est pas prévu pour Fabric.");
        for (var i = 0; i < manifest.Files.Count; i++)
        {
            var entry = manifest.Files[i];
            if (entry.Sha256.Length != 64 || entry.Sha256.Any(c => !Uri.IsHexDigit(c)))
                throw new InvalidOperationException($"SHA-256 invalide pour {entry.Path}");
            await SyncFileAsync(gameRoot, entry.Path, entry.Url, entry.Sha256, HashAlgorithmName.SHA256, $"mod ({i + 1}/{manifest.Files.Count})");
        }
        for (var i = 0; i < manifest.ModrinthVersions.Count; i++)
        {
            var versionId = manifest.ModrinthVersions[i];
            if (string.IsNullOrWhiteSpace(versionId) || versionId.Any(c => !char.IsAsciiLetterOrDigit(c)))
                throw new InvalidOperationException("Identifiant de version Modrinth invalide.");
            StatusText.Text = $"Vérification des mods Cobblemon ({i + 1}/{manifest.ModrinthVersions.Count})…";
            var modrinth = await _http.GetFromJsonAsync<ModrinthVersion>($"https://api.modrinth.com/v2/version/{versionId}")
                ?? throw new InvalidOperationException($"Modrinth n'a pas renvoyé la version {versionId}.");
            if (modrinth.VersionType != "release" || !modrinth.GameVersions.Contains(_config.MinecraftVersion) || !modrinth.Loaders.Contains("fabric", StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Le mod {modrinth.Name} n'est pas une release Fabric compatible avec Minecraft {_config.MinecraftVersion}.");
            var jar = modrinth.Files.FirstOrDefault(f => f.Primary && f.Filename.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                ?? modrinth.Files.FirstOrDefault(f => f.Filename.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Aucun fichier .jar trouvé pour {modrinth.Name}.");
            if (!Uri.TryCreate(jar.Url, UriKind.Absolute, out var jarUri) || jarUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException($"Modrinth a retourné une adresse non HTTPS pour {jar.Filename}.");
            var expectedHash = jar.Hashes.TryGetValue("sha512", out var sha512) ? sha512 : jar.Hashes.GetValueOrDefault("sha1");
            var hashAlgorithm = jar.Hashes.ContainsKey("sha512") ? HashAlgorithmName.SHA512 : HashAlgorithmName.SHA1;
            var expectedLength = hashAlgorithm == HashAlgorithmName.SHA512 ? 128 : 40;
            if (string.IsNullOrWhiteSpace(expectedHash) || expectedHash.Length != expectedLength || expectedHash.Any(c => !Uri.IsHexDigit(c)))
                throw new InvalidOperationException($"Modrinth n'a pas fourni d'empreinte {hashAlgorithm.Name} valide pour {jar.Filename}.");
            await SyncFileAsync(gameRoot, $"mods/{jar.Filename}", jar.Url, expectedHash, hashAlgorithm, $"Cobblemon ({i + 1}/{manifest.ModrinthVersions.Count})");
        }
    }

    private async Task SyncFileAsync(string gameRoot, string relativePath, string fileUrl, string expectedHash, HashAlgorithmName hashAlgorithm, string progressLabel)
    {
        var destination = ResolveChildPath(gameRoot, relativePath);
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var fileUri) || fileUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"URL HTTPS invalide pour {relativePath}");
        if (File.Exists(destination) && (await HashFileAsync(destination, hashAlgorithm)).Equals(expectedHash, StringComparison.OrdinalIgnoreCase)) return;
        StatusText.Text = $"Synchronisation {progressLabel} : {Path.GetFileName(relativePath)}";
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var bytes = await _http.GetByteArrayAsync(fileUri);
        var actualHash = Convert.ToHexString(HashBytes(bytes, hashAlgorithm));
        if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Le fichier téléchargé ne correspond pas à l'empreinte attendue : {relativePath}");
        var temp = destination + ".download";
        await File.WriteAllBytesAsync(temp, bytes);
        File.Move(temp, destination, true);
    }

    private static async Task<string> HashFileAsync(string path, HashAlgorithmName algorithm)
    {
        await using var stream = File.OpenRead(path);
        var hash = algorithm == HashAlgorithmName.SHA512 ? await SHA512.HashDataAsync(stream)
            : algorithm == HashAlgorithmName.SHA1 ? await SHA1.HashDataAsync(stream)
            : await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash);
    }

    private static byte[] HashBytes(byte[] bytes, HashAlgorithmName algorithm) =>
        algorithm == HashAlgorithmName.SHA512 ? SHA512.HashData(bytes)
        : algorithm == HashAlgorithmName.SHA1 ? SHA1.HashData(bytes)
        : SHA256.HashData(bytes);

    private static string ResolveChildPath(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidOperationException("Chemin de fichier modpack invalide.");
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(fullRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Le manifeste contient un chemin hors du dossier du jeu.");
        return full;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choisir le dossier Minecraft" };
        if (dialog.ShowDialog() == true) DirectoryBox.Text = dialog.FolderName;
    }
    private void RamSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (RamLabel is not null) RamLabel.Text = $"{(int)e.NewValue} Go"; }
    private void SaveSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_userSettingsFile)!);
        File.WriteAllText(_userSettingsFile, JsonSerializer.Serialize(new UserSettings(DirectoryBox.Text, (int)RamSlider.Value * 1024), JsonOptions()));
    }
    private void SetBusy(bool value) { _busy = value; PlayButton.IsEnabled = !value; PlayButton.Content = value ? "INSTALLATION EN COURS…" : "INSTALLER ET JOUER"; }
    private static bool IsPlaceholder(string value) => string.IsNullOrWhiteSpace(value) || value.Contains("REMPLACER", StringComparison.OrdinalIgnoreCase) || value.Contains("example.invalid", StringComparison.OrdinalIgnoreCase);
    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private void OpenConfig_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_configFile)) File.WriteAllText(_configFile, JsonSerializer.Serialize(_config, JsonOptions()));
        Process.Start(new ProcessStartInfo(_configFile) { UseShellExecute = true });
    }
}

public sealed class LauncherConfig
{
    public string ServerName { get; set; } = "CobblemonHarcadiaAcademy";
    public string MinecraftVersion { get; set; } = "1.21.1";
    public string FabricLoaderVersion { get; set; } = "latest";
    public int DefaultRamMb { get; set; } = 6144;
    public int MinimumRamMb { get; set; } = 4096;
    public int MaximumRamMb { get; set; } = 16384;
    public string GameDirectory { get; set; } = "";
    public string ServerAddress { get; set; } = "REMPLACER_PAR_ADRESSE_DU_SERVEUR";
    public int ServerPort { get; set; } = 25565;
    public string ModpackManifestUrl { get; set; } = "https://jovanidu63-netizen.github.io/CobblemonHarcadiaAcademy/modpack/mods.json";
    public string LogoPath { get; set; } = "Assets/logo.svg";
}
public sealed class ModpackManifest { public string Version { get; set; } = "1.0.0"; public string Minecraft { get; set; } = ""; public string Loader { get; set; } = ""; public List<string> ModrinthVersions { get; set; } = []; public List<ModpackFile> Files { get; set; } = []; }
public sealed class ModpackFile { public string Path { get; set; } = ""; public string Url { get; set; } = ""; public string Sha256 { get; set; } = ""; }
public sealed class ModrinthVersion
{
    [JsonPropertyName("version_type")] public string VersionType { get; set; } = "";
    [JsonPropertyName("game_versions")] public List<string> GameVersions { get; set; } = [];
    public List<string> Loaders { get; set; } = [];
    public List<ModrinthFile> Files { get; set; } = [];
    public string Name { get; set; } = "";
}
public sealed class ModrinthFile
{
    public string Filename { get; set; } = "";
    public string Url { get; set; } = "";
    public bool Primary { get; set; }
    public Dictionary<string, string> Hashes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
public sealed record UserSettings(string GameDirectory, int RamMb);
