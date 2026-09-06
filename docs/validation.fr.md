# Expérience Companion et validation

**Français** | [English](validation.md)

[Retour au README](../README.fr.md)

La recherche et la fraîcheur sont disponibles depuis la release 2026.0906.00.
L'accueil, le guide de démarrage, l'historique, l'aperçu avec copie et les
vérifications de mises à jour appartiennent à la prochaine version (**Unreleased**).
**Recherche** exige Cortex 2026.0906.00 ou ultérieur. Avec une CLI compatible
plus ancienne, les autres fonctions restent disponibles ; la recherche est
désactivée avec une explication invitant à mettre à jour.

## Rechercher un document

Saisir une question, choisir éventuellement une section et un type de source,
puis sélectionner **Rechercher**. La section correspond à un filtre exact.
Les résultats affichent le titre, le chemin, la date connue et un extrait.
Sélectionner un résultat puis **Ouvrir la source** pour consulter le document.
Une cible absente ou refusée laisse ce bouton désactivé.

Les filtres avancés sont initialement repliés. Sélectionner un résultat affiche
son aperçu. **Copier l'extrait et sa référence** copie l'extrait, le titre et
la source. Modifier la requête ou les filtres efface les anciens résultats et
la sélection. **Interrompre** ou Échap annule la recherche en cours.

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

Une indexation locale seule expose sa date de réussite sans confirmer la
fraîcheur Confluence. L'accueil recommande la synchronisation après une tentative
plus récente non réussie ou lorsque publication et indexation divergent.
Le guide valide la configuration enregistrée, jamais un brouillon de chemin.

## Recette de la prochaine version

| Zone | Scénario | Résultat attendu |
|---|---|---|
| Accueil et guide | Démarrer sans documents configurés, puis enregistrer un chemin valide | Action vers Réglages, puis synchronisation ; configuration enregistrée vérifiée |
| Confluence facultatif | Indexer uniquement les documents locaux | Date d'indexation disponible, sans inventer une fraîcheur Confluence |
| Recherche | Copier un résultat, puis changer un filtre | Copie de l'extrait/titre/référence ; anciens résultats et possibilité de copie effacés |
| Historique | Consulter des opérations réussies, partielles, interrompues et sans résultat final | Issue propre à chaque opération ; absence de preuve jamais présentée comme une réussite |
| Erreurs | Consulter une liste échantillonnée ou un enregistrement illisible | Limites explicites ; les autres enregistrements lisibles restent disponibles |
| Mises à jour | Vérifier en ligne puis sans réseau | Version stable affichée en cas de succès ; ancienne disponibilité effacée après échec et nouvelle tentative possible |
| Téléchargement | Ouvrir le téléchargement officiel | Page de l'installeur commun dans le navigateur, sans exécution automatique |

L'historique lit les deux magasins de workers Companion conservés, pas les
commandes CLI externes. Les fichiers publiés regroupent ajouts et modifications.
Les anciennes opérations et certaines collectes Confluence n'ont pas de compteurs
détaillés. Les liens de reprise ouvrent les écrans actuels sans rejouer une commande.

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
synthétique, exerce l'action du guide, vérifie les filtres repliés et le parcours
Tab après leur ouverture, puis rend Accueil, Recherche et Historique à la taille
minimale en 96, 144 et 192 DPI. Le rendu Historique insère un résultat synthétique
après le chargement vide ; les tests du lecteur valident séparément les vrais
formats de fichiers des workers.

Ces captures vérifient le rendu WPF, pas les changements d'échelle physiques de
Windows. Avant de déclarer une couverture manuelle complète, vérifier encore :

- tout le parcours clavier, jusqu'à l'ouverture de la source ;
- les annonces Narrator pour une recherche vide, dégradée ou en échec ;
- le déplacement entre écrans configurés à 100 %, 150 % et 200 % ;
- le redimensionnement avec des titres, extraits et messages longs.

La suite automatisée ne prétend pas avoir validé ces derniers parcours.
