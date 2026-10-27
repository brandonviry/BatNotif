# BatNotif

Application Windows discrète qui tourne en arrière-plan et **protège la santé de la batterie** de ton PC portable. Elle te prévient, **avec la musique de ton choix**, quand la batterie sort de sa zone de confort :

- **trop basse** (défaut ≤ 20 %, sur batterie) → branche le chargeur ;
- **trop haute** (défaut ≥ 80 %, sur secteur) → débranche le chargeur.

Le rappel se répète tant que la situation n'est pas corrigée, avec une notification maison (fenêtre toujours au premier plan, qui ne vole pas le focus), un bouton **Pause** et des réglages persistants.

---

## État d'avancement

🟡 **Phase de conception.** Aucun code pour l'instant.

- [x] Idée et cahier des charges rédigés → [`idee.md`](idee.md)
- [x] Spec Kit installé (développement piloté par les specs)
- [x] Constitution du projet v1.1.0 → [`.specify/memory/constitution.md`](.specify/memory/constitution.md)
- [x] Spécification du MVP → [`specs/001-batnotif-mvp/spec.md`](specs/001-batnotif-mvp/spec.md)
- [ ] Clarifications complémentaires (`/speckit-clarify`, optionnel)
- [ ] Plan technique (`/speckit-plan`)
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

## Stack prévue

| Brique | Choix |
|---|---|
| Langage / UI | C# + WPF, .NET 8 (`net8.0-windows10.0.19041.0`) |
| Batterie | `Windows.Devices.Power.Battery` (WinRT) |
| Icône tray | `H.NotifyIcon.Wpf` |
| Audio | `NAudio` |
| Réglages | JSON (`System.Text.Json`) |

Aucun droit administrateur requis.

---

## Compiler et lancer

*À compléter dès que le projet .NET existe.*

---

## Organisation du dépôt

| Chemin | Contenu |
|---|---|
| [`idee.md`](idee.md) | L'idée et le cahier des charges (le quoi / pourquoi détaillé) |
| [`CLAUDE.md`](CLAUDE.md) | Règles de travail pour Claude Code (Spec Kit, workflow, logs) |
| [`.specify/memory/constitution.md`](.specify/memory/constitution.md) | Constitution : les 6 principes non négociables du projet |
| `specs/` | Spécifications, plans et tâches par feature (générés avec Spec Kit) |
| `.specify/` | Infrastructure Spec Kit (constitution, templates, scripts) |
| `.claude/skills/` | Commandes `/speckit-*` pour Claude Code |
