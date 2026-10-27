# BatNotif

Application Windows discrète qui tourne en arrière-plan et **protège la santé de la batterie** de ton PC portable. Elle te prévient, **avec la musique de ton choix**, quand la batterie sort de sa zone de confort :

- **trop basse** (défaut ≤ 20 %, sur batterie) → branche le chargeur ;
- **trop haute** (défaut ≥ 80 %, sur secteur) → débranche le chargeur.

L'alerte ne s'arrête pas tant que la situation n'est pas corrigée : notification maison persistante (toujours au premier plan, sans voler le focus, sauf en plein écran où elle interrompt la session) et son en boucle, avec **Snooze** et **Pause**, et des réglages persistants.

---

## État d'avancement

🟢 **Première version livrable atteinte** (58 tâches sur 65) : alertes continues (y compris l'alerte renforcée en plein écran), fenêtre de réglages (seuils, sons, volume, démarrage avec Windows) enregistrés dans `%AppData%\BatNotif\settings.json`, **snooze 15 min et pause 15 min / 1 h / indéfinie** (persistée). Icône d'état, clic gauche et raccourci `Ctrl+Alt+B` inclus. Toutes les user stories sont validées manuellement ; reste la finition (exe autonome, mesures, docs).

- [x] Idée et cahier des charges rédigés → [`idee.md`](idee.md)
- [x] Spec Kit installé (développement piloté par les specs)
- [x] Constitution du projet v1.2.0 → [`.specify/memory/constitution.md`](.specify/memory/constitution.md)
- [x] Spécification du MVP → [`specs/001-batnotif-mvp/spec.md`](specs/001-batnotif-mvp/spec.md)
- [x] Plan technique → [`specs/001-batnotif-mvp/plan.md`](specs/001-batnotif-mvp/plan.md) (+ `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)
- [x] Tâches → [`specs/001-batnotif-mvp/tasks.md`](specs/001-batnotif-mvp/tasks.md) (65 tâches)
- [ ] Implémentation (`/speckit-implement`)
  - [x] Phases 1–2 : solution, projets, types du moteur, journal, source batterie, instance unique
  - [x] US1 : alertes continues (jalon interne) — validée manuellement (M1, M2, M3, M7, M8, M15)
  - [x] US2 : réglages — validée manuellement (M9, M10, M11, M12, M16, SC-005)
  - [x] US3 : snooze / pause → **première version livrable** — validée manuellement (M4, M5, M6)
  - [x] US4 : tray complet, raccourci — validée manuellement (M13, M14)
  - [ ] Finition : exe autonome, mesures, docs

---

## Fonctionnalités prévues (MVP)

- Surveillance en arrière-plan avec une icône dans la zone de notification
- Détection du niveau de batterie et du secteur (branché / débranché)
- Seuils bas et haut réglables, avec hystérésis pour éviter les alertes en boucle
- Notification visuelle personnalisée et son choisi pour chaque type d'alerte
- Alerte continue (notification persistante + son en boucle) jusqu'à ce que tu branches / débranches, avec snooze 15 min et pause (15 min / 1 h / indéfinie)
- En plein écran (jeu, vidéo), l'alerte est renforcée et interrompt la session
- Réglages sauvegardés dans `%AppData%\BatNotif\settings.json`
- Démarrage automatique avec Windows (option)

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
| Distribution | `.exe` unique autonome (win-x64), rien à installer |

Aucun droit administrateur requis. Choix justifiés dans [`specs/001-batnotif-mvp/research.md`](specs/001-batnotif-mvp/research.md).

---

## Compiler et lancer

Prérequis : SDK .NET 10 (version fixée par `global.json`).

```powershell
dotnet build BatNotif.sln      # compile les 3 projets
dotnet test BatNotif.sln       # lance les tests du moteur
```

Lancer l'application. Une pastille apparaît dans la zone de notification : sa couleur indique l'état (vert normal, rouge alerte basse, ambre alerte haute, gris pause) et elle affiche le niveau. **Clic gauche** : afficher / masquer la fenêtre. **Clic droit** : Ouvrir, Pause 15 min / 1 h / indéfinie ou Reprendre, Quitter. **`Ctrl+Alt+B`** rouvre la fenêtre depuis n'importe où (utile quand Windows 11 cache l'icône dans le tiroir `^`).

```powershell
dotnet run --project src/BatNotif.App
# Debug uniquement : simuler une alerte sans modifier le code
dotnet run --project src/BatNotif.App -- --high 63   # alerte haute si branché et batterie ≥ 63 %
dotnet run --project src/BatNotif.App -- --low 65    # alerte basse si sur batterie et batterie ≤ 65 %
```

Journal : `%AppData%\BatNotif\logs\`. La publication de l'exe sera documentée à l'étape de finition. Procédure complète prévue : [`specs/001-batnotif-mvp/quickstart.md`](specs/001-batnotif-mvp/quickstart.md).

---

## Organisation du dépôt

| Chemin | Contenu |
|---|---|
| [`idee.md`](idee.md) | L'idée et le cahier des charges (le quoi / pourquoi détaillé) |
| [`CLAUDE.md`](CLAUDE.md) | Règles de travail pour Claude Code (périmètre, Spec Kit, workflow, logs) |
| [`.specify/memory/constitution.md`](.specify/memory/constitution.md) | Constitution : les 6 principes non négociables du projet |
| `specs/` | Spécifications, plans et tâches par feature (générés avec Spec Kit) |
| `.specify/` | Infrastructure Spec Kit (constitution, templates, scripts) |
| `.claude/skills/` | Commandes `/speckit-*` pour Claude Code |
