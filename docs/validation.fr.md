# Recherche, fraîcheur et validation visuelle

**Français** | [English](validation.md)

[Retour au README](../README.fr.md)

Ces ajouts sont disponibles dans les sources non encore publiées. L'écran
**Recherche** exige une version de Cortex fournissant `search --json` ; une CLI
plus ancienne affiche un échec explicite.

## Rechercher un document

Saisir une question, choisir éventuellement une section et un type de source,
puis sélectionner **Rechercher**. La section correspond à un filtre exact.
Les résultats affichent le titre, le chemin, la date connue et un extrait.
Sélectionner un résultat puis **Ouvrir la source** pour consulter le document.
Une cible absente ou refusée laisse ce bouton désactivé.

L'absence de résultat, le classement dégradé, le dépassement du délai et une
erreur de transport ou de contrat sont des états distincts. Une recherche en
échec efface les résultats précédents. Reconnecter Cortex dans **Réglages** annule
la recherche de l'ancien contexte ; fermer la fenêtre annule aussi la recherche.
Cette règle diffère des workers de synchronisation qui peuvent survivre à la fenêtre.

## Lire la fraîcheur des données

**Base locale** distingue la dernière collecte réussie, la génération publiée et
la dernière génération dont l'indexation réussie a été observée dans les résultats
durables de Companion. Le lecteur examine au plus 100 répertoires d'exécution récents.
Un run local plus récent, incomplet ou en échec, empêche de confirmer la concordance.

Une preuve absente, illisible ou incomplète apparaît comme non confirmée. Les
synchronisations lancées hors de Companion ne sont pas déduites d'une date de
fichier. Ce suivi est un historique d'observation, pas une inspection indépendante
du contenu courant de Chroma.

## Valider localement

Après restauration des dépendances du dépôt :

```powershell
dotnet test CortexCompanion.sln -c Release --no-restore
dotnet build tests/CortexCompanion.LockProbe/CortexCompanion.LockProbe.csproj --no-restore
python tests/interop/search_contract_proof.py
python tests/interop/renderer_differential_proof.py
python tests/interop/lock_interop_proof.py
```

Les preuves Python attendent un dépôt `Cortex` voisin et ses dépendances installées.
Le workflow `release-pair` accepte deux SHA complets, contrôle les révisions et
exécute ces preuves. Il documente un couple de sources, pas les octets d'un installeur.

Pour conserver les captures du test WPF :

```powershell
$env:CORTEX_VISUAL_ARTIFACTS = Join-Path $PWD 'local/visual-validation'
dotnet test CortexCompanion.sln -c Release --no-restore --filter FullyQualifiedName~MainWindowSmokeTests
```

Le test ouvre la fenêtre réelle avec une configuration temporaire et du contenu
synthétique, sélectionne **Recherche**, vérifie Tab de la requête vers la section,
puis rend la fenêtre à sa taille minimale en 96, 144 et 192 DPI.

Ces captures vérifient le rendu WPF, pas les changements d'échelle physiques de
Windows. Avant de déclarer une couverture manuelle complète, vérifier encore :

- tout le parcours clavier, jusqu'à l'ouverture de la source ;
- les annonces Narrator pour une recherche vide, dégradée ou en échec ;
- le déplacement entre écrans configurés à 100 %, 150 % et 200 % ;
- le redimensionnement avec des titres, extraits et messages longs.

La suite automatisée ne prétend pas avoir validé ces derniers parcours.
