# Serveur Harcadia Academy (Fabric 1.21.1)

Ce dossier contient les fichiers de préparation du serveur Cobblemon sur une machine Windows disposant de Java 21. L’hébergeur choisi est HelloServ et l’adresse communiquée est `game06.helloserv.fr:25512`. Le port est réglé dans `server.properties`; vérifiez qu’il correspond toujours au port affiché dans le panel avant le démarrage.

## Paquet serveur

1. Dans GitHub, ouvrir **Actions → Prepare HelloServ server package → Run workflow**. L’action construit un paquet Fabric 1.21.1 avec Cobblemon 1.8.0 et Fabric API 0.116.11, épinglés aux mêmes versions que le launcher.
2. Télécharger l’artefact ZIP de l’action.
3. Dans le panneau HelloServ, confirmer Minecraft 1.21.1/Fabric et le port attribué `25512`, puis téléverser les fichiers extraits. Si le panel demande le fichier de démarrage, sélectionner `fabric-server-launch.jar` ou le profil Fabric proposé par HelloServ.
4. Lire `eula.txt` et passer `eula=false` à `eula=true` seulement si tu acceptes la licence. Ajouter ton pseudo Minecraft à la whitelist avant le premier démarrage.
5. Démarrer puis vérifier les journaux du panel. Le manifeste client est servi par le site GitHub Pages en HTTPS.

Le manifeste est aussi intégré au ZIP serveur sous `modpack/mods.json`; les mods y sont épinglés et munis de leurs empreintes SHA-256. La copie client du manifeste est `site/modpack/mods.json`. N’ajoute pas de mods client-only au serveur.

Ne partagez jamais le port d’administration RCON sur Internet. Configurez une whitelist avant d’ouvrir le serveur au public.
