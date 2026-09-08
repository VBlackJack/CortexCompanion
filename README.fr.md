# Cortex Companion

[English](README.md) | **Français**

Cortex Companion est l'interface Windows de Cortex. Elle s'adresse aux personnes
qui ne devraient pas avoir à modifier un fichier TOML ni à ouvrir un terminal pour
la configuration, la synchronisation ou la planification courantes.

## Fenêtre de périmètre lisible et délais honnêtes (2026.0907.00)

La fenêtre de choix du périmètre affichait ses trois options dans la couleur de
texte du système sur le fond sombre, faute de style et de couleur déclarés, le
seul style RadioButton du thème étant nommé. Elles sont de nouveau lisibles, et
une garde automatique exige désormais que chaque RadioButton et CheckBox d'une
vue déclare sa propre couleur.

Un délai dépassé le dit maintenant. Le chargement d'une arborescence ne signale
plus une panne de connexion, et le conseil nomme le délai maximal réellement
sélectionnable. Toute valeur hors 15, 30, 60 et 120 secondes revient à 30, donc
un simple "augmentez le délai" ne pouvait pas être suivi.

Le Cortex apparié 2026.0907.00 mesure un périmètre Confluence par comptages indexés :
ajouter une source dans un grand espace répond en une seconde environ au lieu de
dépasser le délai sans rien afficher.

## Langue de l'interface

Companion parle français et anglais. Il suit la langue de Windows par défaut, et les
Réglages proposent un choix explicite qui la remplace. Chaque langue est nommée dans sa
propre langue.

Le changement s'applique au prochain démarrage, et le réglage le dit. Plusieurs valeurs
localisées sont des formats analysés une seule fois à la première lecture : les basculer
en place laisserait une partie de l'interface dans la langue précédente.

L'anglais est le jeu de ressources neutre, donc une machine dont la langue n'est pas
fournie par Companion lit l'anglais plutôt que des clés de ressources brutes.

## Gérer les sources Confluence (2026.0906.02)

**Mes sources** apparaît en premier. Recherchez un espace ou un titre de page,
ouvrez l'original dans Confluence ou choisissez **Modifier la sélection**.
**Ajouter une source** ouvre le formulaire de lien lorsque vous en avez besoin.

L'éditeur conserve les pages cochées. **Charger les pages de cet espace** affiche
l'arborescence distante avec recherche ; les pages implicitement incluses par
une racine sont indiquées. La portée choisie s'applique à toutes les racines de
cet espace. Le catalogue est limité à 10 000 pages et ne présente jamais une
lecture partielle comme complète. En cas d'échec, les choix restent éditables.

Avant confirmation, le résumé distingue les racines ajoutées/retirées et les
documents réellement affectés, sous-pages et recouvrements compris. Une mesure
indisponible est signalée ; aucun total de sous-pages n'est inventé.
**Enregistrer et mettre à jour** collecte toutes les sources puis indexe après
réussite. **Enregistrer pour plus tard** conserve seulement la sélection.

Les cartes indiquent **À appliquer**, **Mise à jour en cours**, **Disponible dans
la recherche**, **Action requise** ou une disponibilité non vérifiée. Ces états
sont conservateurs et concernent la génération commune aux sources : la
sélection publiée et l'identité de génération indexée doivent correspondre.
Un succès de collecte seul ne signifie jamais que la recherche est à jour.

**Retirer de Cortex** ne supprime aucun original Confluence. Le dernier retrait
de la session peut être annulé si aucun octet de configuration n'a changé ;
une nouvelle mise à jour peut être nécessaire après restauration. Les erreurs
restent visibles avec **Réessayer**, **Reconnecter Confluence** et l'accès au
résultat détaillé. La reconnexion demande une date d'expiration valide et
reprend l'action échouée sans ajouter une source en double.

Ce parcours nécessite Cortex et Companion 2026.0906.02 ou ultérieurs. La recette WPF et les tests
automatiques ne remplacent pas une observation d'un nouvel utilisateur.

## Accueil et premiers pas

Ces améliorations sont disponibles dans la release appariée Cortex et Companion 2026.0906.01.

**Accueil** affiche les dates observées d'indexation et de collecte Confluence,
la prochaine collecte planifiée, l'activité et la prochaine action utile. Le
guide propose quatre étapes : choisir les documents, connecter éventuellement
Confluence, synchroniser, puis essayer une recherche. La configuration est
vérifiée depuis son état enregistré, jamais depuis un brouillon de chemin.
L'indexation indique une réussite observée dans les opérations conservées.

**Recherche** replie initialement les filtres avancés. Sélectionner un résultat
affiche son aperçu et permet de copier l'extrait avec son titre et sa référence.
Modifier les critères efface la sélection précédente et désactive sa copie.

**Historique** présente les opérations manuelles et planifiées conservées pour
le compte Windows courant. Les commandes CLI externes et les opérations déjà
purgées ne sont pas incluses. Les workers conservent normalement dix opérations
terminées par catégorie ; la lecture est plafonnée à 100 dossiers par catégorie.
Une opération sans résultat final reste non confirmée. Les compteurs disponibles
respectent le contrat Cortex : les fichiers publiés regroupent ajouts et
modifications. Les anciens résultats et les collectes Confluence peuvent ne pas
contenir ces compteurs. Une liste d'erreurs échantillonnée est signalée.
Les actions de reprise ouvrent l'écran opérationnel actuel sans relancer
automatiquement une ancienne opération.

La carte **Versions et mises à jour**, dans Accueil, contacte GitHub uniquement
sur demande. Elle compare Companion en cours d'exécution et Cortex connecté avec
la release stable officielle. Le bouton de téléchargement ouvre la page de
l'installeur commun ; aucune installation n'est lancée automatiquement.

Consulter le [guide de validation](docs/validation.fr.md) pour les contrôles
automatisés et les scénarios de recette manuelle restants.

## Installer et synchroniser les documents locaux

1. Télécharger l'installeur Windows unique depuis la
   [dernière release Cortex](https://github.com/VBlackJack/Cortex/releases/latest).
2. Lancer l'installeur, puis ouvrir **Cortex Companion** depuis le menu Démarrer.
   La version publique actuelle n'est pas signée. Si Microsoft Defender SmartScreen
   affiche un avertissement, comparer d'abord l'empreinte SHA-256 de l'installeur
   avec celle publiée dans la release, puis seulement choisir **Informations
   complémentaires** et **Exécuter quand même**.
3. Ouvrir **Réglages**. Companion détecte normalement le `cortex.exe` de la même
   installation Cortex, y compris le dossier parent utilisé par l'installeur
   combiné. Choisir un dossier de base de connaissances existant, puis sélectionner
   **Enregistrer le dossier**.
4. Ouvrir **Pages Confluence** et coller le lien HTTPS d'une page ou d'un espace.
   Sélectionner **Voir les documents à ajouter**. Companion déduit l'instance et
   l'espace. Les liens courts et `viewpage.action` nécessitent une clé d'espace.
5. Si la connexion manque ou est refusée, saisir le jeton et sa date d'expiration
   dans le même écran, puis **Enregistrer le jeton et continuer**. Le lien reste saisi.
   Le jeton est enregistré directement dans le Gestionnaire d'identifiants Windows.
6. Choisir cette page, cette page et ses sous-pages, ou tout l'espace. Vérifier le
   nombre de pages mesuré et le stockage approximatif, puis confirmer le nombre.
   Un lien d'espace désigne sa page d'accueil ; tout l'espace inclut aussi les pages
   hors de son arborescence. Annuler laisse la configuration active intacte.
7. Sélectionner **Collecter maintenant**. Companion collecte toutes les sources
   Confluence configurées, puis indexe seulement après une collecte réussie.
   Suivre le statut et utiliser **Rechercher un document** quand l'index est à jour.
8. Les sources existantes sont visibles dans **Mes sources**. Seules les options
   avancées du convertisseur restent repliées. L'installeur combiné fournit le convertisseur.
   Les actions indépendantes restent accessibles dans **Base locale**.

L'action de synchronisation locale exécute `cortex sync --json` ; elle n'exige
aucune configuration Confluence. L'action de collecte Confluence est distincte et
passe toujours `--force`, car la cadence ne doit jamais primer sur un geste
explicite de l'utilisateur.

L'écran **Pages Confluence** crée lui-même la configuration initiale. L'utilisateur
n'a aucun fichier TOML à trouver ni à modifier. Les configurations existantes
conservent leurs valeurs avancées exactes et passent par le même chemin de mutation
compare-and-swap. Les configurations créées par des versions qui omettaient
`console_path` sont réparées atomiquement au premier chargement, après que le
convertisseur embarqué a passé la même sonde.

## Interrompre une opération

Tant qu'une opération est vivante, **Interrompre** apparaît à côté des deux actions
de collecte. Le bouton demande confirmation, annonce la conséquence exacte, puis
arrête le worker détaché et le processus Cortex qu'il possède. Une opération
interrompue est enregistrée comme interrompue, pas comme un échec : la génération
publiée précédente reste intacte et l'index local est complété à la synchronisation
suivante. L'arrêt ne touche que le worker dont l'identité de processus enregistrée
correspond encore, de sorte qu'un identifiant de processus réutilisé n'est jamais
tué.

Fermer la fenêtre pendant une opération ne l'arrête pas. Companion le dit et demande
confirmation avant, car le worker survit à la fenêtre : seul l'affichage de la
progression est perdu.

## Clavier

| Raccourci | Action |
|---|---|
| `F5` | Recharger l'écran courant |
| `Ctrl+S` | Enregistrer et connecter, sur l'écran Réglages |
| `Entree` | Valide le champ en cours de saisie : URL de page ou d'espace, PAT, dossier, chemin, heure |
| `Tab` / `Maj+Tab` | Passer d'un contrôle à l'autre ; le contrôle actif est entouré |
| `Echap` | Annuler la boîte de dialogue de confirmation ouverte |

## Ce que l'utilisateur peut faire

- se connecter à `cortex.exe` par découverte automatique au premier lancement ou par
  un sélecteur de fichiers natif ;
- choisir le dossier de base de connaissances de Cortex ;
- synchroniser des documents locaux sans configuration Confluence ;
- initialiser Confluence depuis une seule URL de page, sans éditer de TOML ;
- comparer les périmètres page seule, arborescence et espace entier avant de les
  enregistrer ;
- suivre les longues collectes à travers l'énumération, la préparation, la
  conversion et la publication ;
- ouvrir la génération courante et voir la rétention de stockage configurée ;
- interrompre une collecte en cours, avec la conséquence annoncée avant l'arrêt ;
- éventuellement consulter les pages Confluence configurées, stocker un identifiant
  Confluence, lancer la collecte Confluence et gérer la tâche planifiée Windows
  qu'elle possède.

L'application affiche sa fenêtre avant d'exécuter la poignée de main bornée avec
Cortex. Si Cortex est absent, incompatible ou indisponible, l'écran Réglages reste
actionnable tandis que les commandes de mutation restent désactivées. Les
diagnostics de démarrage inattendus sont écrits sous
`%LOCALAPPDATA%\CortexCompanion\logs`. Si la fenêtre ne peut pas être créée, la
boîte de dialogue fatale affiche aussi le type et le message de l'exception afin
que le support puisse identifier l'échec sans devoir d'abord retrouver le journal.
La barrière de release ouvre la fenêtre principale complète pour détecter les
liaisons WPF invalides avant publication.

## Commandes Cortex lentes

L'écran Réglages propose un délai borné pour la CLI Cortex : 15, 30, 60 ou
120 secondes. La valeur par défaut est 30 secondes, y compris quand Companion charge
un fichier de réglages créé par une version antérieure. Choisir une valeur plus
longue avant d'utiliser **Enregistrer et connecter** sur une machine où `cortex.exe`
met plus de temps à répondre.

La valeur choisie est partagée par la poignée de main de compatibilité
`cortex.exe --version`, les lectures et écritures de configuration Cortex, et les
lectures et résolutions de pages Confluence. Si une lecture dépasse la limite,
Companion garde les mutations fail-closed et renvoie l'utilisateur vers les Réglages
plutôt que d'affirmer que la CLI a refusé la demande. Les journaux de dépassement
indiquent la durée configurée et la durée écoulée, sans enregistrer les arguments de
commande ni de secret.

## Propriétaire de la configuration

Companion stocke le chemin de son `cortex.exe` et le délai CLI partagé borné dans
`%LOCALAPPDATA%\CortexCompanion\settings.json`. Le réglage de la base de
connaissances est lu et modifié exclusivement par le contrat versionné
`cortex config get/set --json` en compare-and-swap.

Le choix de source confirmé crée `%APPDATA%\Cortex\confluence.toml` par le
même écrivain verrouillé, validé et atomique que les mutations de pages ultérieures.
Elle refuse d'écraser un fichier apparu entre-temps. Le fichier contient l'URL de
base inférée, l'expiration déclarée du PAT, la liste blanche explicite d'espaces, la
cible locale, la classification et le chemin validé du convertisseur embarqué. Il ne
contient jamais le PAT.

Companion refuse d'écrire un `base_url` qui n'est pas en `https` hors bouclage,
exactement la règle que Cortex applique à la lecture : les deux ne peuvent donc pas
diverger sur ce qu'est une configuration valide.

Le PAT Confluence n'est jamais écrit dans `settings.json` ni dans `CONFLUENCE.toml`.
Le champ masqué des Réglages l'écrit directement dans le `credential_target` déclaré
par la configuration Confluence validée, ou dans la valeur par défaut `cortex-spike`
de Cortex quand ce fichier n'existe pas encore. Cortex et Companion utilisent la même
entrée générique du Gestionnaire d'identification Windows, protégée par DPAPI pour le
compte Windows courant. Si une configuration ultérieure désigne une autre cible,
enregistrer de nouveau le PAT pour la cible affichée.

## Construire et tester

Prérequis : Windows et le SDK .NET 10.

```powershell
dotnet restore CortexCompanion.sln --locked-mode
dotnet list CortexCompanion.sln package --vulnerable --include-transitive
dotnet format CortexCompanion.sln --verify-no-changes --no-restore
dotnet build CortexCompanion.sln -c Release --no-restore -warnaserror
dotnet test CortexCompanion.sln -c Release --no-build --no-restore
```

Les valeurs de mise en page, les couleurs et les textes destinés à l'utilisateur sont
gardés par des tests : les vues ne peuvent porter ni taille brute ni couleur
hexadécimale, chaque ressource de thème nommée par une vue doit exister, chaque
chaîne exposée doit résoudre vers une vraie ressource, et chaque paire de texte doit
passer WCAG AA tandis que les bordures et les anneaux de focus passent le plancher
non textuel de 3:1.

Le dépôt refuse les déclarations C# implicites `var`. Activer la barrière locale de
pre-push une fois par clone :

```powershell
git config core.hooksPath .githooks
```

### Preuves d'interopérabilité

Cinq scripts Python sous `tests/interop/` prouvent le contrat que Companion partage
avec la CLI Cortex sur une même machine. Le workflow `interoperability` des deux
dépôts les exécute contre `main` du dépôt partenaire à chaque push et pull request.
Pour un changement coordonné, son paramètre manuel `peer_ref` permet de choisir la
branche ou le commit partenaire. Les scripts restent exécutables localement avec
les deux dépôts côte à côte et couvrent les schémas TOML v1, v2 et v3.

- `lock_interop_proof.py` prend le verrou de configuration depuis chaque côté à tour
  de rôle et attend que l'autre côté soit refusé (la sonde C# sort avec le code `2`,
  le `filelock` Python expire).
- `renderer_differential_proof.py` rend la même configuration par la sonde C# et par
  le rendu Python de `confluence_writer`, puis compare les octets.
- `search_contract_proof.py` transmet les résultats JSON Python au vrai parseur C#
  et vérifie les trois modes de recherche, y compris le texte Unicode.
- `confluence_contract_proof.py` transmet les documents Python resolve, preview, pages,
  catalog et status aux enregistrements C# qui les lisent, et vérifie chaque valeur qui
  traverse, pas seulement l'acceptation du document.
- `cli_surface_proof.py` fait le chemin inverse : la sonde C# capture chaque ligne de
  commande Cortex que le bureau construit, depuis le code qui la construit, et Cortex
  l'analyse avec le parseur qui l'exécute, en s'arrêtant avant toute exécution. Une
  sous-commande renommée, une option parente déplacée après sa sous-commande ou un
  drapeau retiré échouent ici plutôt que sur le bureau de l'utilisateur.

Les cinq exigent `dotnet` dans le PATH, une compilation Debug de
`tests/CortexCompanion.LockProbe` et un interpréteur Python avec les dépendances de
Cortex installées ; toutes sauf la preuve du verrou attendent en plus un clone de
Cortex à côté de ce dépôt, dans `../Cortex`. La preuve des lignes de commande lit
`CORTEX_CHECKOUT` quand un clone de branche vit ailleurs. Chaque script affiche
`PROOF RESULT=PASS` et sort avec `0` en cas de succès.

```powershell
dotnet build tests/CortexCompanion.LockProbe/CortexCompanion.LockProbe.csproj
python tests/interop/lock_interop_proof.py
python tests/interop/renderer_differential_proof.py
python tests/interop/search_contract_proof.py
python tests/interop/confluence_contract_proof.py
python tests/interop/cli_surface_proof.py
```

Le workflow manuel `release-pair` valide une paire exacte de commits sources.
Fournir les SHA complets de 40 caractères `cortex_sha` et `companion_sha` ; cette
preuve ne certifie pas un installeur. Voir le [guide de validation](docs/validation.fr.md).

## Charge utile de release Windows

La charge utile autonome canonique utilisée par l'installeur Cortex combiné est :

```powershell
dotnet publish src/CortexCompanion/CortexCompanion.csproj `
  -c Release `
  --no-restore `
  -p:PublishProfile=win-x64 `
  -o artifacts/publish/win-x64
```

`artifacts/publish/win-x64/CortexCompanion.exe --version` écrit exactement la CalVer
de build sur la sortie standard redirigée et se termine avec le code `0`.
L'installeur Cortex utilise ce contrat fail-closed avant d'accepter la charge utile
Companion.

Pendant la désinstallation, l'installeur combiné exécute
`CortexCompanion.exe --uninstall-cleanup`. Ce mode processus seul se termine par `0`
avec `cleanup=deleted`, `cleanup=absent` ou `cleanup=foreign-preserved`. Il ne
supprime que la tâche `\CortexCompanion\Ingestion-doc` exacte dont le jeton de
propriété immuable est encore présent ; une tâche absente ou étrangère n'est jamais
supprimée. Les échecs de lecture du planificateur se terminent par `1` avec
`cleanup=failed`.

La charge utile contient aussi les avis de redistribution `LICENSE.txt`,
`ThirdPartyNotices.txt`, `WPF-LICENSE.txt`, `WPF-ThirdPartyNotices.txt` et
`Tomlyn-LICENSE.txt`, ainsi que le `CortexCompanion-LICENSE.txt` de l'application.
La publication échoue si un avis source requis est absent.

## Politique de confirmation

Une confirmation explicite est exigée avant toute opération qui retire ou remplace un
état, y compris la suppression d'une page, le changement de mode de collecte,
l'interruption d'une opération en cours et la suppression de la tâche planifiée.
Annuler et fermer la fenêtre restent des actions non autorisantes.

Sous licence Apache 2.0.

## Recherche et fraîcheur

L'écran **Recherche** affiche les extraits indexés et propose des filtres et l'ouverture de la source. **Base locale** distingue la génération publiée de la dernière indexation réussie observée. Voir [les limites et les commandes de validation](docs/validation.fr.md).

La recherche est disponible avec toute version de Cortex acceptée par Companion, le
plancher accepté étant la CLI livrée avec lui. **Interrompre** ou Échap
annule la recherche. Modifier ses critères efface les résultats obsolètes ; les
derniers critères exécutés restent affichés. Une source impossible à ouvrir présente
une explication. La fraîcheur est directement visible dans Recherche et Base locale,
avec un accès à la synchronisation et aux réglages.

Recharger ou revisiter les réglages et la programmation conserve les saisies non
enregistrées du dossier, de l'heure et de la fréquence. Pour la première authentification
Confluence, ouvrir **Pages Confluence** et remplir la connexion dans le même écran.
