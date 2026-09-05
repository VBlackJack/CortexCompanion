# Cortex Companion

[English](README.md) | **Francais**

Cortex Companion est l'interface Windows de Cortex. Elle s'adresse aux personnes
qui ne devraient pas avoir a modifier un fichier TOML ni a ouvrir un terminal pour
la configuration, la synchronisation ou la planification courantes.

## Installer et synchroniser les documents locaux

1. Telecharger l'installeur Windows unique depuis la
   [derniere release Cortex](https://github.com/VBlackJack/Cortex/releases/latest).
2. Lancer l'installeur, puis ouvrir **Cortex Companion** depuis le menu Demarrer.
   La version publique actuelle n'est pas signee. Si Microsoft Defender SmartScreen
   affiche un avertissement, comparer d'abord l'empreinte SHA-256 de l'installeur
   avec celle publiee dans la release, puis seulement choisir **Informations
   complementaires** et **Executer quand meme**.
3. Ouvrir **Reglages**. Companion detecte normalement le `cortex.exe` de la meme
   installation Cortex, y compris le dossier parent utilise par l'installeur
   combine. Choisir un dossier de base de connaissances existant, puis selectionner
   **Enregistrer le dossier**.
4. Pour utiliser Confluence, saisir le PAT sous **Reglages > Authentification
   Confluence** et selectionner **Enregistrer le PAT**. Sur une installation
   neuve, Companion utilise la cible d'identifiant Windows par defaut de Cortex,
   `cortex-spike`.
5. Ouvrir **Pages Confluence** et coller l'URL complete de la premiere page.
   L'URL doit etre en `https` : une instance `http` en clair est refusee, car le
   jeton d'acces circule en en-tete sur chaque requete. Les adresses de bouclage
   restent acceptees pour une instance de test locale. Companion detecte
   l'instance et l'espace des que l'URL les contient. Choisir la date d'expiration
   du PAT et la classification, puis selectionner **Initialiser et ajouter la
   page**. Companion mesure les perimetres page seule, arborescence et espace
   entier avant d'ecrire le choix. L'arborescence est preselectionnee quand la page
   a des descendants. Les anciennes URL `viewpage.action` et les URL courtes
   exigent la saisie de la cle d'espace.
6. Verifier le nombre de pages mesure, le stockage approximatif, la racine
   physique d'ingestion et la retention des generations. Le `target` configure est
   un prefixe d'index logique, pas un dossier dans la base de connaissances
   choisie.
7. L'installeur Cortex combine fournit deja le convertisseur Confluence sans
   fenetre. Aucun chemin n'est requis. La surcharge developpeur reste sous les
   options avancees repliees et n'est acceptee qu'apres une sonde de capacite
   machine de cinq secondes ; le `ConfluenceRAGBuilder.exe` fenetre est refuse.
8. Ouvrir **Base locale**. **Synchroniser les documents locaux** indexe la base de
   connaissances locale et la generation d'ingestion publiee courante ;
   **Collecter Confluence** lance immediatement une collecte manuelle et affiche la
   phase et la progression chiffree. Les deux actions sont cote a cote sur la carte
   principale.
9. Utiliser **Ouvrir la generation courante** pour inspecter les documents publies
   immuables. Un perimetre etroit mais reussi signale les descendants exclus et
   propose un basculement en un clic vers la collecte d'arborescence.

L'action de synchronisation locale execute `cortex sync --json` ; elle n'exige
aucune configuration Confluence. L'action de collecte Confluence est distincte et
passe toujours `--force`, car la cadence ne doit jamais primer sur un geste
explicite de l'utilisateur.

L'ecran **Pages Confluence** cree lui-meme la configuration initiale. L'utilisateur
n'a aucun fichier TOML a trouver ni a modifier. Les configurations existantes
conservent leurs valeurs avancees exactes et passent par le meme chemin de mutation
compare-and-swap. Les configurations creees par des versions qui omettaient
`console_path` sont reparees atomiquement au premier chargement, apres que le
convertisseur embarque a passe la meme sonde.

## Interrompre une operation

Tant qu'une operation est vivante, **Interrompre** apparait a cote des deux actions
de collecte. Le bouton demande confirmation, annonce la consequence exacte, puis
arrete le worker detache et le processus Cortex qu'il possede. Une operation
interrompue est enregistree comme interrompue, pas comme un echec : la generation
publiee precedente reste intacte et l'index local est complete a la synchronisation
suivante. L'arret ne touche que le worker dont l'identite de processus enregistree
correspond encore, de sorte qu'un identifiant de processus reutilise n'est jamais
tue.

Fermer la fenetre pendant une operation ne l'arrete pas. Companion le dit et demande
confirmation avant, car le worker survit a la fenetre : seul l'affichage de la
progression est perdu.

## Clavier

| Raccourci | Action |
|---|---|
| `F5` | Recharger l'ecran courant |
| `Ctrl+S` | Enregistrer et connecter, sur l'ecran Reglages |
| `Entree` | Valide le champ en cours de saisie : URL de page ou d'espace, PAT, dossier, chemin, heure |
| `Tab` / `Maj+Tab` | Passer d'un controle a l'autre ; le controle actif est entoure |
| `Echap` | Annuler la boite de dialogue de confirmation ouverte |

## Ce que l'utilisateur peut faire

- se connecter a `cortex.exe` par decouverte automatique au premier lancement ou par
  un selecteur de fichiers natif ;
- choisir le dossier de base de connaissances de Cortex ;
- synchroniser des documents locaux sans configuration Confluence ;
- initialiser Confluence depuis une seule URL de page, sans editer de TOML ;
- comparer les perimetres page seule, arborescence et espace entier avant de les
  enregistrer ;
- suivre les longues collectes a travers l'enumeration, la preparation, la
  conversion et la publication ;
- ouvrir la generation courante et voir la retention de stockage configuree ;
- interrompre une collecte en cours, avec la consequence annoncee avant l'arret ;
- eventuellement consulter les pages Confluence configurees, stocker un identifiant
  Confluence, lancer la collecte Confluence et gerer la tache planifiee Windows
  qu'elle possede.

L'application affiche sa fenetre avant d'executer la poignee de main bornee avec
Cortex. Si Cortex est absent, incompatible ou indisponible, l'ecran Reglages reste
actionnable tandis que les commandes de mutation restent desactivees. Les
diagnostics de demarrage inattendus sont ecrits sous
`%LOCALAPPDATA%\CortexCompanion\logs`. Si la fenetre ne peut pas etre creee, la
boite de dialogue fatale affiche aussi le type et le message de l'exception afin
que le support puisse identifier l'echec sans devoir d'abord retrouver le journal.
La barriere de release ouvre la fenetre principale complete pour detecter les
liaisons WPF invalides avant publication.

## Commandes Cortex lentes

L'ecran Reglages propose un delai borne pour la CLI Cortex : 15, 30, 60 ou
120 secondes. La valeur par defaut est 30 secondes, y compris quand Companion charge
un fichier de reglages cree par une version anterieure. Choisir une valeur plus
longue avant d'utiliser **Enregistrer et connecter** sur une machine ou `cortex.exe`
met plus de temps a repondre.

La valeur choisie est partagee par la poignee de main de compatibilite
`cortex.exe --version`, les lectures et ecritures de configuration Cortex, et les
lectures et resolutions de pages Confluence. Si une lecture depasse la limite,
Companion garde les mutations fail-closed et renvoie l'utilisateur vers les Reglages
plutot que d'affirmer que la CLI a refuse la demande. Les journaux de depassement
indiquent la duree configuree et la duree ecoulee, sans enregistrer les arguments de
commande ni de secret.

## Proprietaire de la configuration

Companion stocke le chemin de son `cortex.exe` et le delai CLI partage borne dans
`%LOCALAPPDATA%\CortexCompanion\settings.json`. Le reglage de la base de
connaissances est lu et modifie exclusivement par le contrat versionne
`cortex config get/set --json` en compare-and-swap.

La carte Pages du premier lancement cree `%APPDATA%\Cortex\confluence.toml` par le
meme ecrivain verrouille, valide et atomique que les mutations de pages ulterieures.
Elle refuse d'ecraser un fichier apparu entre-temps. Le fichier contient l'URL de
base inferee, l'expiration declaree du PAT, la liste blanche explicite d'espaces, la
cible locale, la classification et le chemin valide du convertisseur embarque. Il ne
contient jamais le PAT.

Companion refuse d'ecrire un `base_url` qui n'est pas en `https` hors bouclage,
exactement la regle que Cortex applique a la lecture : les deux ne peuvent donc pas
diverger sur ce qu'est une configuration valide.

Le PAT Confluence n'est jamais ecrit dans `settings.json` ni dans `CONFLUENCE.toml`.
Le champ masque des Reglages l'ecrit directement dans le `credential_target` declare
par la configuration Confluence validee, ou dans la valeur par defaut `cortex-spike`
de Cortex quand ce fichier n'existe pas encore. Cortex et Companion utilisent la meme
entree generique du Gestionnaire d'identification Windows, protegee par DPAPI pour le
compte Windows courant. Si une configuration ulterieure designe une autre cible,
enregistrer de nouveau le PAT pour la cible affichee.

## Construire et tester

Prerequis : Windows et le SDK .NET 10.

```powershell
dotnet restore CortexCompanion.sln --locked-mode
dotnet list CortexCompanion.sln package --vulnerable --include-transitive
dotnet format CortexCompanion.sln --verify-no-changes --no-restore
dotnet build CortexCompanion.sln -c Release --no-restore -warnaserror
dotnet test CortexCompanion.sln -c Release --no-build --no-restore
```

Les valeurs de mise en page, les couleurs et les textes destines a l'utilisateur sont
gardes par des tests : les vues ne peuvent porter ni taille brute ni couleur
hexadecimale, chaque ressource de theme nommee par une vue doit exister, chaque
chaine exposee doit resoudre vers une vraie ressource, et chaque paire de texte doit
passer WCAG AA tandis que les bordures et les anneaux de focus passent le plancher
non textuel de 3:1.

Le depot refuse les declarations C# implicites `var`. Activer la barriere locale de
pre-push une fois par clone :

```powershell
git config core.hooksPath .githooks
```

### Preuves d'interoperabilite

Deux scripts Python sous `tests/interop/` prouvent le contrat que Companion partage
avec la CLI Cortex sur une meme machine. Le workflow `interoperability` des deux
depots les execute contre `main` du depot partenaire a chaque push et pull request.
Pour un changement coordonne, son parametre manuel `peer_ref` permet de choisir la
branche ou le commit partenaire. Les scripts restent executables localement avec
les deux depots cote a cote et couvrent les schemas TOML v1, v2 et v3.

- `lock_interop_proof.py` prend le verrou de configuration depuis chaque cote a tour
  de role et attend que l'autre cote soit refuse (la sonde C# sort avec le code `2`,
  le `filelock` Python expire).
- `renderer_differential_proof.py` rend la meme configuration par la sonde C# et par
  le rendu Python de `confluence_writer`, puis compare les octets.

Les deux exigent `dotnet` dans le PATH, une compilation Debug de
`tests/CortexCompanion.LockProbe` et un interpreteur Python avec les dependances de
Cortex installees ; la preuve du rendu attend en plus un clone de Cortex a cote de ce
depot, dans `../Cortex`. Chaque script affiche `PROOF RESULT=PASS` et sort avec `0`
en cas de succes.

```powershell
dotnet build tests/CortexCompanion.LockProbe/CortexCompanion.LockProbe.csproj
python tests/interop/lock_interop_proof.py
python tests/interop/renderer_differential_proof.py
```

## Charge utile de release Windows

La charge utile autonome canonique utilisee par l'installeur Cortex combine est :

```powershell
dotnet publish src/CortexCompanion/CortexCompanion.csproj `
  -c Release `
  --no-restore `
  -p:PublishProfile=win-x64 `
  -o artifacts/publish/win-x64
```

`artifacts/publish/win-x64/CortexCompanion.exe --version` ecrit exactement la CalVer
de build sur la sortie standard redirigee et se termine avec le code `0`.
L'installeur Cortex utilise ce contrat fail-closed avant d'accepter la charge utile
Companion.

Pendant la desinstallation, l'installeur combine execute
`CortexCompanion.exe --uninstall-cleanup`. Ce mode processus seul se termine par `0`
avec `cleanup=deleted`, `cleanup=absent` ou `cleanup=foreign-preserved`. Il ne
supprime que la tache `\CortexCompanion\Ingestion-doc` exacte dont le jeton de
propriete immuable est encore present ; une tache absente ou etrangere n'est jamais
supprimee. Les echecs de lecture du planificateur se terminent par `1` avec
`cleanup=failed`.

La charge utile contient aussi les avis de redistribution `LICENSE.txt`,
`ThirdPartyNotices.txt`, `WPF-LICENSE.txt`, `WPF-ThirdPartyNotices.txt` et
`Tomlyn-LICENSE.txt`, ainsi que le `CortexCompanion-LICENSE.txt` de l'application.
La publication echoue si un avis source requis est absent.

## Politique de confirmation

Une confirmation explicite est exigee avant toute operation qui retire ou remplace un
etat, y compris la suppression d'une page, le changement de mode de collecte,
l'interruption d'une operation en cours et la suppression de la tache planifiee.
Annuler et fermer la fenetre restent des actions non autorisantes.

Sous licence Apache 2.0.
