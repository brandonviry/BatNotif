# BatNotif

Application Windows discrète qui tourne en arrière-plan et **protège la santé de la batterie** de ton PC portable. Elle te prévient, **avec la musique de ton choix**, quand la batterie sort de sa zone de confort :

- **trop basse** (défaut ≤ 20 %, sur batterie) → branche le chargeur ;
- **trop haute** (défaut ≥ 80 %, sur secteur) → débranche le chargeur.

L'alerte ne s'arrête pas tant que la situation n'est pas corrigée : notification maison persistante (toujours au premier plan, sans voler le focus, sauf en plein écran où elle interrompt la session) et son en boucle, avec **Snooze** et **Pause**, et des réglages persistants.

---

## État d'avancement

🟡 **Phase de conception.** Aucun code pour l'instant.

- [x] Idée et cahier des charges rédigés → [`idee.md`](idee.md)
- [x] Spec Kit installé (développement piloté par les specs)
- [x] Constitution du projet v1.2.0 → [`.specify/memory/constitution.md`](.specify/memory/constitution.md)
- [x] Spécification du MVP → [`specs/001-batnotif-mvp/spec.md`](specs/001-batnotif-mvp/spec.md)
- [x] Plan technique → [`specs/001-batnotif-mvp/plan.md`](specs/001-batnotif-mvp/plan.md) (+ `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)
- [ ] Tâches (`/speckit-tasks`)
- [ ] Implémentation (`/speckit-implement`)

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

*À compléter dès que le projet .NET existe.* Prérequis : SDK .NET 10. Procédure prévue : [`specs/001-batnotif-mvp/quickstart.md`](specs/001-batnotif-mvp/quickstart.md).

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
