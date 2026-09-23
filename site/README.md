# Site Harcadia

Site vitrine statique, sans serveur web ni base de données. Hébergement gratuit possible avec GitHub Pages dans un dépôt public GitHub Free, d’abord à une adresse GitHub Pages. Le nom `CobblemonHarcadiaAcademy.com` a été choisi mais n’est pas acheté : vérifie sa disponibilité et son prix auprès d’un registrar. L’hébergement Minecraft sera une dépense distincte. Un sous-domaine `play.votredomaine` pourra pointer vers ce serveur.

## Préparation

1. Le workflow GitHub Actions compile le launcher Windows x64 et le copie dans `site/downloads/` avant de publier Pages. Il faut pousser ces changements dans la branche `main` pour obtenir le nouvel EXE sur le site.
2. Le launcher est paramétré pour Minecraft Java 1.21.1 / Fabric et télécharge Cobblemon 1.8.0 + Fabric API 0.116.11 depuis les versions officielles Modrinth, après vérification d’empreinte.
3. Dans GitHub, lancer l’action **Prepare HelloServ server package**, télécharger son artefact, puis envoyer son contenu dans le panneau HelloServ. Confirmer `game06.helloserv.fr:25512`, accepter la licence dans `eula.txt`, autoriser ton compte dans la whitelist et tester une vraie connexion. L’accès au panneau n’est pas fourni dans ce projet.
4. Quand le domaine sera acheté, l’ajouter dans les paramètres Pages, puis configurer chez le registrar les DNS indiqués par GitHub. Vérifier le domaine avant de modifier les DNS et conserver l’enregistrement d’e-mail du domaine.

L’hébergement gratuit du site ne rend pas le serveur de jeu gratuit et le domaine `.com` n’est pas gratuit. Pour la mise en ligne immédiate, l’adresse `github.io` fonctionne sans nom de domaine personnalisé.

## Dons et boutique

La page n’encaisse encore rien et ne collecte aucune donnée. Avant d’ajouter un prestataire de paiement, publier les prix et contenus avant achat, un contact direct pour le support, et des conditions de vente/remboursement adaptées au pays. Conserver l’historique des paiements. Pour démarrer, privilégier des dons sans avantage exclusif au donateur et des cosmétiques. Les privilèges gameplay payants ne doivent ni gâcher l’expérience ni donner d’avantage compétitif. Ne conditionner ni l’accès au serveur ni les mods à des achats externes. Les règles officielles Minecraft demandent aussi une mention visible de non-affiliation.

Le script `server/Install-Server.ps1` crée les fichiers serveur et publie un manifeste SHA-256 dans `server/modpack/mods.json` et `site/modpack/mods.json`. L’adresse actuelle du manifeste est `https://jovanidu63-netizen.github.io/CobblemonHarcadiaAcademy/modpack/mods.json`.
