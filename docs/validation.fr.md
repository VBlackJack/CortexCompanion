# Expérience Companion et validation

**Français** | [English](validation.md)

[Retour au README](../README.fr.md)

La recherche et la fraîcheur sont disponibles depuis la release 2026.0906.00.
L'accueil, le guide de démarrage, l'historique, l'aperçu avec copie et les
vérifications de mises à jour sont disponibles depuis la version 2026.0906.01.
**Recherche** suit la poignée de main de démarrage : toute CLI acceptée par
Companion porte le contrat de recherche, il n'existe donc plus de plancher de
version propre à la recherche.

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
python tests/interop/confluence_contract_proof.py
python tests/interop/cli_surface_proof.py
python tests/interop/renderer_differential_proof.py
python tests/interop/lock_interop_proof.py
```

Les preuves Python attendent un dépôt `Cortex` voisin et ses dépendances installées.
La preuve des lignes de commande analyse chaque ligne que le bureau construit avec les
parseurs Cortex qui l'exécutent : une sous-commande renommée ou une option déplacée
échoue là, pas à l'exécution.
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


## Ajout Confluence par lien (2026.0906.01)

Le formulaire accepte une page ou un espace. La connexion dans le même écran conserve le lien. La prévisualisation utilise un TOML temporaire sans secret ; seul le choix mesuré et confirmé passe au writer atomique CAS. Les tests couvrent la première configuration, un nouvel espace, l'annulation, les erreurs d'authentification et distantes, les éditions concurrentes et les origines étrangères. Collecte et indexation ont des statuts distincts ; une collecte non réussie interdit l'indexation automatique. Les liens anciens exigent encore une clé d'espace. Les racines existantes conservent leur mode sauf choix explicite de tout l'espace.

Les liens d'espace exigent Cortex 2026.0906.01 ou ultérieur. Le smoke visuel rend la fenêtre minimale et la confirmation à 100 %, 150 % et 200 % de résolution raster. Il ne remplace pas un essai avec un compte Confluence réel ni un changement natif de DPI.

## Mes sources - recette 2026.0909.00

- Vérifier les cartes visibles, les liens navigateur et les titres longs.
- Ouvrir la sélection pré-remplie ; annuler ne doit rien écrire.
- Changer la portée et décocher une racine ; vérifier le résumé avant/après.
- Retirer une racine couverte ailleurs : la confirmation indique la couverture,
  ou son indisponibilité. La configuration temporaire est effacée.
- Coller une page que la source collecte déjà, une fois listée directement et une
  fois sous un sous-arbre suivi : la fenêtre de périmètre le dit, et après le choix
  du périmètre la revue de fusion propose élargir et remplacer avec les documents
  que chacun ajoute et retire. Annuler n'écrit rien ; une source qui collecte
  l'espace entier ne propose que remplacer.
- Retirer la dernière source, collecter puis indexer avec le Cortex apparié.
  Les originaux Confluence restent intacts ; la recherche suit la génération indexée.
- Les tests de service couvrent CAS, lecture seule, annulation, serveur étranger,
  dernière source et liste manquante. Le smoke WPF rend l'éditeur et les cartes.
  Les images à 100/150/200 % sont des rendus, pas une recette Narrator ou DPI natif.

## Parcours Mes sources et récupération (2026.0909.00)

- Vérifier l'ordre liste puis formulaire, le filtre par titre et l'état sans résultat.
- Charger une arborescence, rechercher une sous-page et vérifier que ses parents
  restent visibles et que le filtre ne change pas les cases cochées.
- Comparer changements de racines et documents effectifs dans la confirmation.
- Annuler un retrait avant toute autre écriture ; provoquer ensuite un conflit
  et vérifier que l'annulation ne restaure pas aveuglément une sauvegarde.
- Enregistrer pour plus tard : aucune collecte. Enregistrer et mettre à jour :
  collecte, puis indexation uniquement après code de réussite observé.
- Vérifier que la génération collectée non indexée reste À appliquer.
- Échouer une opération : feedback persistant, réessai et reconnexion accessibles.
  Reconnexion : date valide requise, périmètre préservé, reprise sans doublon.
- Tester clavier, lecteur d'écran et DPI natif en recette humaine. Les rendus
  automatiques 100/150/200 % couvrent la disposition, pas ces usages.

## Régressions de récupération UX

- Pendant une lecture différée du catalogue, Annuler et Échap interrompent la lecture sans fermer l'éditeur ni changer le brouillon ; fermer l'éditeur annule la requête du processus.
- Soumettre un lien supplémentaire invalide, réessayer et vérifier la restauration de la portée, des racines et du lien. Corriger puis confirmer avant toute écriture. Une configuration modifiée ne doit pas restaurer un brouillon obsolète.
- Charger un état en erreur avec action requise : la consigne apparaît au-dessus des actions de synchronisation sans ouvrir les options avancées. Un état sain suivant la retire.
- Modifier le dossier documentaire et appuyer sur Ctrl+S dans son champ : la sauvegarde du dossier est appelée. L'indication de modification non enregistrée disparaît après réussite.

## Parcours documentaire unifié

- Mettre à jour une configuration locale seule : aucun worker Confluence ne doit démarrer.
- Avec Confluence : collecter avant d'indexer ; échec d'authentification, erreur distante ou annulation doit empêcher l'indexation suivante.
- Modifier le dossier sans enregistrer : Mes sources doit toujours afficher le chemin enregistré.
- Une ancienne indexation réussie ne doit pas masquer une collecte échouée sur Accueil.
- Comparer racines sélectionnées et pages incluses observées avant/après filtrage ; le résumé et les actions restent visibles pendant le défilement.
- Redimensionner l'aperçu à la souris et au clavier ; les deux panneaux restent utilisables. Un résultat vide propose une récupération sans effacer la requête.
- Le raccourci de connexion initial ouvre les options avancées et place le focus sur le chemin Cortex. Vérifier les réglages connectés avec ces options repliées.

## Contrôles de régression de l’audit (2026.0909.00)

La suite automatique couvre les réponses de 1 000/10 000 pages, le dépassement de sortie explicite, la disponibilité locale seule, le changement de dossier et la relecture de la preuve, l’annulation avant confirmation et le nombre borné de lignes d’arbre réalisées. Le traitement conserve la configuration uniquement si les observations avant/après indexation concordent. Un ancien traitement sans preuve nécessite une nouvelle mise à jour.

Recette manuelle : changer de dossier enregistré, redémarrer, le mettre à jour et vérifier sa disponibilité ; annuler la préparation sans charger le catalogue au préalable ; parcourir et filtrer un grand arbre en vérifiant la conservation des sélections. DPI natif et Narrateur restent à vérifier manuellement.
