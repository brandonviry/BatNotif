# BatNotif

Application Windows discrète qui tourne en arrière-plan et **protège la santé de la batterie** de ton PC portable. Elle te prévient, **avec la musique de ton choix**, quand la batterie sort de sa zone de confort :

- **trop basse** (défaut ≤ 20 %, sur batterie) → branche le chargeur ;
- **trop haute** (défaut ≥ 80 %, sur secteur) → débranche le chargeur.

L'alerte ne s'arrête pas tant que la situation n'est pas corrigée : notification maison persistante (toujours au premier plan, sans voler le focus, sauf en plein écran où elle interrompt la session) et son en boucle, avec **Snooze** et **Pause**, et des réglages persistants.

---

## État d'avancement

✅ **MVP terminé** (65 tâches sur 65) : alertes continues (y compris l'alerte renforcée en plein écran), fenêtre de réglages, snooze et pause persistée, icône d'état, raccourci `Ctrl+Alt+B`, exe autonome en deux versions. 94 tests automatiques verts, toutes les user stories validées manuellement, CPU au repos ≈ 0,004 %.

- [x] Idée et cahier des charges rédigés → [`idee.md`](idee.md)
- [x] Spec Kit installé (développement piloté par les specs)
- [x] Constitution du projet v1.3.0 → [`.specify/memory/constitution.md`](.specify/memory/constitution.md)
- [x] Spécification du MVP → [`specs/001-batnotif-mvp/spec.md`](specs/001-batnotif-mvp/spec.md)
- [x] Plan technique → [`specs/001-batnotif-mvp/plan.md`](specs/001-batnotif-mvp/plan.md) (+ `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)
- [x] Tâches → [`specs/001-batnotif-mvp/tasks.md`](specs/001-batnotif-mvp/tasks.md) (65 tâches)
- [x] Implémentation (`/speckit-implement`)
  - [x] Phases 1–2 : solution, projets, types du moteur, journal, source batterie, instance unique
  - [x] US1 : alertes continues (jalon interne) — validée manuellement (M1, M2, M3, M7, M8, M15)
  - [x] US2 : réglages — validée manuellement (M9, M10, M11, M12, M16, SC-005)
  - [x] US3 : snooze / pause → **première version livrable** — validée manuellement (M4, M5, M6)
  - [x] US4 : tray complet, raccourci — validée manuellement (M13, M14)
  - [x] Finition : exe autonome en deux versions (standard / compact), mesures CPU et mémoire, journal vérifié, docs

---

## Fonctionnalités (MVP)

- Surveillance en arrière-plan avec une pastille dans la zone de notification (couleur selon l'état + niveau)
- Détection du niveau de batterie et du secteur, y compris le chargeur trop faible qui laisse la batterie se vider
- Seuils bas et haut réglables (5–95 %), avec une marge de 2 % pour éviter les alertes en boucle
- Notification maison et son choisi pour chaque type d'alerte (MP3 / WAV), ou sons intégrés
- Alerte continue (notification persistante + son en boucle) jusqu'à ce que tu branches / débranches, avec snooze 15 min et pause 15 min / 1 h / indéfinie
- En plein écran (jeu, vidéo), l'alerte est agrandie et interrompt la session
- Alerte immédiate au démarrage et au réveil de veille si la batterie est déjà hors zone
- Réglages et pause conservés entre les redémarrages ; démarrage automatique avec Windows (option)
- Raccourci global `Ctrl+Alt+B`, instance unique, aucun droit administrateur

Le détail complet est dans [`idee.md`](idee.md).

---

## Stack

| Brique | Choix |
|---|---|
| Langage / UI | C# + WPF, **.NET 10 LTS** (`net10.0-windows10.0.19041.0`) |
| Moteur d'alerte | Projet `BatNotif.Core` (`net10.0`), sans dépendance Windows, testé avec xUnit |
| Batterie / secteur | WinRT `PowerManager` |
| Icône tray | `H.NotifyIcon.Wpf` |
| Audio | `NAudio` |
| Réglages | JSON (`System.Text.Json`) dans `%AppData%\BatNotif\` |
| Distribution | `.exe` unique autonome (win-x64), rien à installer, en deux versions (voir ci-dessous) |

Aucun droit administrateur requis. Choix justifiés dans [`specs/001-batnotif-mvp/research.md`](specs/001-batnotif-mvp/research.md).

---

## Utiliser BatNotif

BatNotif existe en **deux versions** du même programme : choisis-en une.

| Version | Taille du fichier | Mémoire quand il tourne | Pour qui |
|---|---|---|---|
| **standard** (recommandée) | ≈ 160 Mo | ≈ 133 Mo | L'app tourne en permanence : elle consomme moins de mémoire |
| **compact** | ≈ 69 Mo | ≈ 194 Mo | Tu veux le plus petit fichier possible sur le disque |

Dans les deux cas, l'utilisation du processeur au repos est négligeable (≈ 0,004 % mesuré sur 10 min).

Lance `BatNotif.exe` (aucune installation, aucun droit administrateur). Une pastille apparaît dans la zone de notification : sa couleur indique l'état (vert normal, rouge alerte basse, ambre alerte haute, gris pause) et elle affiche le niveau.

- **Clic gauche** : afficher / masquer la fenêtre de réglages.
- **Clic droit** : Ouvrir, Pause 15 min / 1 h / indéfinie ou Reprendre, Quitter.
- **`Ctrl+Alt+B`** : rouvre la fenêtre depuis n'importe où (utile quand Windows 11 cache l'icône dans le tiroir `^`).
- Fermer la fenêtre (✕) ne quitte pas BatNotif : les alertes continuent.

| Emplacement | Contenu |
|---|---|
| `%AppData%\BatNotif\settings.json` | Réglages et pause en cours (un fichier illisible est renommé `settings.corrupt-…json` et les valeurs par défaut sont rétablies) |
| `%AppData%\BatNotif\logs\batnotif-AAAA-MM-JJ.log` | Journal (un fichier par jour, conservé 14 jours) |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` → `BatNotif` | Présent seulement si « Démarrer avec Windows » est coché |

> **Ne pas déranger / Focus Assist** : la notification de BatNotif s'affiche quand même (c'est une fenêtre de l'application, pas une notification Windows), mais le son reste soumis au volume et au mixeur de Windows. Si BatNotif est coupé dans le mixeur, l'alerte sera muette.

---

## Compiler, tester, publier

Prérequis : SDK .NET 10 (version fixée par `global.json`).

```powershell
dotnet build BatNotif.sln      # compile les 3 projets
dotnet test BatNotif.sln       # 94 tests du moteur et des réglages
dotnet run --project src/BatNotif.App

# Debug uniquement : simuler une alerte sans modifier le code (seuils non enregistrés)
dotnet run --project src/BatNotif.App -- --high 63   # alerte haute si branché et batterie ≥ 63 %
dotnet run --project src/BatNotif.App -- --low 65    # alerte basse si sur batterie et batterie ≤ 65 %
```

Publier les deux versions de l'exe autonome (quitter BatNotif d'abord) :

```powershell
./scripts/publish.ps1
# [1/5] nettoyage · [2/5] tests · [3/5] version standard · [4/5] version compact · [5/5] résultat
# → publish\standard\BatNotif.exe (≈ 160 Mo, non compressé)
# → publish\compact\BatNotif.exe  (≈ 69 Mo, compressé)
# Runtime .NET inclus, un seul fichier par version.
```

Si « Démarrer avec Windows » était coché avec une version de développement, lance une fois l'exe publié de ton choix : il remet le démarrage automatique sur lui-même.

Scénarios de validation complets : [`specs/001-batnotif-mvp/quickstart.md`](specs/001-batnotif-mvp/quickstart.md).

---

## Organisation du dépôt

| Chemin | Contenu |
|---|---|
| [`idee.md`](idee.md) | L'idée et le cahier des charges (le quoi / pourquoi détaillé) |
| [`CLAUDE.md`](CLAUDE.md) | Règles de travail pour Claude Code (périmètre, Spec Kit, workflow, logs) |
| [`.specify/memory/constitution.md`](.specify/memory/constitution.md) | Constitution : les 6 principes non négociables du projet |
| `src/BatNotif.Core/` | Moteur d'alerte, réglages, journal : aucune dépendance Windows |
| `src/BatNotif.App/` | Application WPF : batterie Windows, notification, audio, tray, fenêtre |
| `tests/BatNotif.Core.Tests/` | Tests xUnit du moteur et des réglages (temps simulé) |
| `scripts/publish.ps1` | Publication de l'exe autonome dans `publish/` (ignoré par git) |
| `specs/` | Spécifications, plans et tâches par feature (générés avec Spec Kit) |
| `.specify/` | Infrastructure Spec Kit (constitution, templates, scripts) |
| `.claude/skills/` | Commandes `/speckit-*` pour Claude Code |
