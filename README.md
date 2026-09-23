# CobblemonHarcadiaAcademy Launcher

Projet Windows WPF pour le launcher Cobblemon Harcadia Academy, Minecraft Java 1.21.1 et Fabric. Le lanceur installe Fabric, les fichiers Minecraft et Java requis, propose les réglages mémoire et dossier du jeu, puis synchronise les versions listées dans un manifeste HTTPS en vérifiant leurs empreintes.

## Compiler en EXE

Prérequis : Windows 10/11, .NET 9 SDK et connexion Internet pour restaurer les dépendances NuGet et télécharger Minecraft/Fabric. La connexion Microsoft ouvre l’authentification officielle dans WebView2 ; le runtime WebView2 doit être présent sur le PC.

Dans PowerShell, depuis ce dossier :

```powershell
./build.ps1
```

Le binaire autonome sera créé dans `outputs/CobblemonHarcadiaAcademy.exe`. Le premier lancement télécharge les fichiers Minecraft et Fabric. Un SDK .NET n’est pas inclus dans le projet.

## Configuration requise avant distribution

Modifier `src/CobblemonHarcadiaAcademy.Launcher/launcher.config.json` :

1. `serverAddress` et `serverPort` sont déjà réglés sur `game06.helloserv.fr:25512`.
2. `modpackManifestUrl` vise le manifeste HTTPS public du site GitHub Pages.
3. `fabricLoaderVersion` : garder `latest` pour utiliser la version Fabric recommandée pour Minecraft 1.21.1, ou fixer une version testée.
4. Optionnel : `defaultRamMb`, `minimumRamMb`, `maximumRamMb`, `gameDirectory`.

Le manifeste publié est `site/modpack/mods.json`. Il référence Cobblemon 1.8.0 et Fabric API 0.116.11 pour Fabric 1.21.1 via leurs versions Modrinth épinglées. Le launcher vérifie les versions, leur compatibilité et les empreintes SHA-512 avant installation. Les sources sont les pages officielles [Cobblemon 1.8.0](https://modrinth.com/mod/cobblemon/version/YgmyyFcs) et [Fabric API 0.116.11](https://modrinth.com/mod/fabric-api/version/IpaMcBLh). Le script `server/Install-Server.ps1` télécharge les mêmes versions pour le serveur et produit aussi un manifeste SHA-256.

Un manifeste personnalisé peut aussi lister des fichiers HTTPS avec leurs empreintes SHA-256 (voir `modpack.manifest.example.json`) :

```json
{
  "version": "1.0.0",
  "files": [
    {
      "path": "mods/fabric-api.jar",
      "url": "https://downloads.exemple.fr/mods/fabric-api.jar",
      "sha256": "64_CARACTERES_HEXADECIMAUX_DU_SHA256"
    }
  ]
}
```

Chaque chemin est relatif au dossier Minecraft, chaque lien doit être en HTTPS et chaque empreinte doit correspondre exactement au fichier. Le format personnalisé accepte SHA-256. Les versions Modrinth épinglées utilisent les empreintes SHA-512 publiées par Modrinth. Le synchroniseur met à jour les fichiers déclarés sans supprimer les autres fichiers locaux.

## Déploiement nécessaire dans le panneau HelloServ

- HelloServ est l’hébergeur choisi et l’adresse fournie est `game06.helloserv.fr:25512`; elle est déjà inscrite dans la configuration du launcher.
- Il faut encore ouvrir le panneau HelloServ, choisir Fabric 1.21.1, vérifier le port 25512 puis déposer les fichiers serveur avec le gestionnaire de fichiers ou SFTP. Aucun identifiant d’accès HelloServ n’a été fourni, donc le serveur n’a pas pu être installé ni démarré depuis ici.
- Il faudra vérifier que le serveur est en ligne et autorise les connexions avant d’annoncer l’ouverture.
- Le manifeste du modpack et le site vitrine utilisent l’adresse HTTPS gratuite de GitHub Pages. Le site et le serveur de jeu sont des services distincts.
- Logo officiel du serveur et droits d’utilisation. Un emblème temporaire est inclus dans l’interface ; remplacez `Assets/logo.svg` et personnalisez l’en-tête dans `MainWindow.xaml`.
- Le launcher demande une connexion Microsoft et utilise le profil Minecraft Java obtenu pour lancer le jeu. CmlLib conserve la session de compte pour faciliter les connexions suivantes dans `cml_accounts.json` du dossier Minecraft ; le mot de passe Microsoft n’est ni demandé par le launcher ni enregistré par celui-ci.

## Site gratuit et financement du serveur

Le site statique utilise GitHub Pages : `https://jovanidu63-netizen.github.io/CobblemonHarcadiaAcademy/`. Le workflow `.github/workflows/pages.yml` compile le launcher Windows et l’ajoute au site. Il faut pousser les changements de ce projet au dépôt GitHub pour que la compilation et la publication se déclenchent. Le workflow `.github/workflows/server-package.yml` prépare aussi un ZIP serveur téléchargeable dans l’onglet Actions. Le domaine `CobblemonHarcadiaAcademy.com` n’est pas acheté. Le site gratuit n’héberge pas le serveur Minecraft.

Le site contient un emplacement « Boutique bientôt disponible » mais aucun achat n’est activé. Un modèle de départ à faible risque est le don de soutien sans avantage individuel, avec éventuellement des récompenses collectives pour tous. Les avantages gameplay payants ne doivent pas ruiner l’expérience des autres ni donner d’avantage compétitif. Toute offre, ses prix, son contenu et un moyen de contact doivent être affichés avant achat/connexion ; conserver un historique des paiements. Afficher clairement que le serveur n’est pas officiel et n’est ni approuvé ni affilié à Mojang/Microsoft. Relire les [directives officielles Minecraft](https://www.minecraft.net/en-us/usage-guidelines) avant d’activer la boutique.

Les scripts de préparation du serveur sont dans `server/`. HelloServ a été choisi et l’adresse configurée est `game06.helloserv.fr:25512`, mais le serveur doit toujours être installé et activé dans le panneau HelloServ. La boutique ne prend aucun paiement. Avant de la mettre en place, publier les prix, conditions et coordonnées de contact, puis relire les [directives officielles Minecraft](https://www.minecraft.net/en-us/usage-guidelines).

Pour produire le ZIP : pousser le projet sur GitHub, ouvrir **Actions → Prepare HelloServ server package → Run workflow**, puis télécharger l’artefact produit et envoyer son contenu dans le panneau HelloServ. Le fichier `eula.txt` reste réglé sur `false` : lis et accepte la licence toi-même dans le panneau avant le premier démarrage. Configure aussi la whitelist avec ton nom de compte Minecraft pour te donner accès.

## Réglages et données

Le dossier de jeu initial est `%LOCALAPPDATA%\CobblemonHarcadiaAcademy\.minecraft`. Les réglages RAM et dossier sont conservés sous `%LOCALAPPDATA%\CobblemonHarcadiaAcademy\settings.json`. La session Microsoft du compte est conservée par CmlLib dans `cml_accounts.json` du dossier Minecraft.

## Structure

- `src/CobblemonHarcadiaAcademy.Launcher/` : application WPF et code d’installation/lancement.
- `launcher.config.json` : nom/version, mémoire, adresse et URL du manifeste.
- `modpack.manifest.example.json` : exemple de format du manifeste.
- `build.ps1` : publication autonome Windows x64.

La bibliothèque CmlLib.Core (MIT) gère l’installation et le lancement Minecraft/Fabric. CmlLib.Core.Auth.Microsoft gère la connexion Microsoft et le profil Minecraft Java. Références : [dépôt CmlLib.Core](https://github.com/CmlLib/CmlLib.Core), [authentification Microsoft](https://cmllib.github.io/CmlLib.Core-wiki/en/cmllib.core/login-and-sessions/Microsoft-Xbox-Live-Login/) et [FabricInstaller](https://cmllib.github.io/CmlLib.Core/api/CmlLib.Core.ModLoaders.FabricMC.FabricInstaller.html).
