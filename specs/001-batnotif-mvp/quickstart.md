# Quickstart — validation de 001-batnotif-mvp

Guide pour **vérifier** que le MVP fonctionne de bout en bout. Les détails de comportement sont dans
[contracts/](contracts/) et [data-model.md](data-model.md).

## Prérequis

- Windows 10 (19041) ou plus récent, **PC portable avec batterie** (sauf scénario « aucune batterie »).
- SDK .NET 10 (`dotnet --list-sdks` doit afficher une version `10.x`).
- Un fichier MP3 et un fichier WAV quelconques pour tester les sons personnalisés.
- Pour le scénario plein écran : un jeu ou une vidéo en plein écran.

## 1. Compiler et tester (automatique)

Chaque commande affiche ses logs complets (voir `CLAUDE.md` §5) :

```powershell
# [1/3] Restauration des paquets
dotnet restore BatNotif.sln -v normal
# [2/3] Compilation
dotnet build BatNotif.sln -c Debug -v normal
# [3/3] Tests unitaires du moteur
dotnet test BatNotif.sln -c Debug --logger "console;verbosity=detailed"
```

**Attendu** : build sans erreur, tous les tests de `BatNotif.Core.Tests` verts (états, transitions, hystérésis,
debounce, snooze, pause, veille, aucune batterie, réglages corrompus).

## 2. Lancer

```powershell
dotnet run --project src/BatNotif.App -c Debug
```

**Attendu** : icône dans la zone de notification ; fenêtre principale avec le niveau réel et l'état du secteur.

### Simuler une alerte sans modifier le code (build Debug uniquement)

```powershell
# Alerte haute : chargeur branché, seuil haut juste sous le niveau actuel (ex. niveau 64 % → --high 63)
dotnet run --project src/BatNotif.App -c Debug -- --high 63
# Alerte basse : sur batterie, seuil bas juste au-dessus du niveau actuel (ex. niveau 64 % → --low 65)
dotnet run --project src/BatNotif.App -c Debug -- --low 65
```

Les seuils passés en argument ne valent que pour la session, ne sont **pas** enregistrés, et le remplacement est
noté dans le journal. Ces arguments n'existent pas dans l'exe publié (Release).

## 3. Scénarios manuels

| # | Scénario | Étapes | Résultat attendu | Spec |
|---|---|---|---|---|
| M1 | Alerte haute | Chargeur branché, lancer avec `--high <niveau − 1>` (ou régler le seuil haut dans la fenêtre une fois US2 faite) | Notification haut-droit + son haut en boucle, sans perte de focus dans un éditeur de texte | US1, FR-007/011/015 |
| M2 | Fin d'alerte | Pendant M1, débrancher | Notification disparaît, son coupé en ≤ 10 s | US1, FR-008, SC-007 |
| M3 | Alerte continue | Laisser M1 tourner 3 min | Notification toujours là, son sans interruption | FR-010 |
| M4 | Snooze | Cliquer « Snooze 15 min » | Silence ; retour au bout de 15 min si toujours branché | US3, FR-027 |
| M5 | Pause persistée | Pause 1 h, quitter, relancer | Pause toujours active, heure de fin affichée, icône pause | US3, FR-026 |
| M6 | Reprendre | En pause avec condition vraie → Reprendre | Alerte immédiate | FR-025 |
| M7 | Démarrage en zone d'alerte | Quitter, régler seuil haut sous le niveau, brancher, relancer | Alerte immédiate au lancement ; même test débranché → rien | FR-004 |
| M8 | Plein écran | Lancer un jeu / vidéo plein écran, provoquer M1 | Alerte agrandie qui passe devant et interrompt la session, son en boucle | FR-037 |
| M9 | Réglages | Changer seuils, sons, volume, Enregistrer, relancer | Tout restauré ; « Tester » joue chaque son au bon volume | US2, FR-017/020/021 |
| M10 | Validation | Seuil bas 78, seuil haut 80, Enregistrer | Refus + message de contrainte | FR-018 |
| M11 | Son introuvable | Choisir un MP3, enregistrer, le renommer, provoquer une alerte | Son intégré joué + avertissement dans la fenêtre | FR-016 |
| M12 | Réglages corrompus | Quitter, écrire du texte invalide dans `settings.json`, relancer | Démarrage normal, valeurs par défaut, avertissement, fichier `.corrupt-…` créé | FR-021 |
| M13 | Tray & raccourci | Fermer la fenêtre ; `Ctrl+Alt+B` ; clic gauche / droit sur l'icône | App toujours active ; fenêtre réapparaît ; menu Ouvrir / Pause / Reprendre / Quitter | US4 |
| M14 | Instance unique | Lancer l'app deux fois | Une seule icône ; la fenêtre existante s'affiche | FR-032 |
| M15 | Veille | Provoquer M1, mettre en veille, réveiller | Alerte présente en ≤ 10 s après réveil | FR-004, SC-001 |
| M16 | Démarrage Windows | Cocher, fermer la session, rouvrir | App lancée en arrière-plan | FR-022 |
| M17 | Journal | Après les scénarios, ouvrir `%AppData%\BatNotif\logs\` | Chaque transition, alerte, snooze, pause présents | FR-036 |

## 4. Publier l'exe autonome

```powershell
./scripts/publish.ps1
```

**Attendu** : logs `[1/N] … [N/N]`, puis un unique `BatNotif.exe` (win-x64, autonome) dans `publish/`, qui se
lance sur une machine sans .NET installé et sans droits admin.
