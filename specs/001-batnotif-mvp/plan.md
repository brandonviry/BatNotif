# Implementation Plan: BatNotif MVP — notificateur de santé de batterie

**Branch**: `001-batnotif-mvp` | **Date**: 2026-10-27 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-batnotif-mvp/spec.md`

## Summary

Application Windows de zone de notification qui surveille batterie et secteur, et déclenche une **alerte
continue** (notification maison persistante + son en boucle) quand la batterie est trop basse sur batterie ou
trop haute sur secteur, jusqu'à correction, snooze (15 min) ou pause. En plein écran, l'alerte est renforcée et
interrompt la session.

Approche technique (détail et justifications dans [research.md](research.md)) : **C# / WPF sur .NET 10 LTS**,
choisi après réévaluation neutre face à WinUI 3 et WinForms. La logique d'alerte vit dans un projet
**`BatNotif.Core`** sans dépendance Windows, testé avec un temps simulé ; l'application WPF ne fait que
brancher les sources système (WinRT `PowerManager`), l'affichage, l'audio (NAudio) et le tray
(H.NotifyIcon.Wpf). Distribution en **`.exe` unique autonome**, sans installeur ni droits admin.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (LTS, support jusqu'en novembre 2028)

**Primary Dependencies**: WPF (plateforme), WinRT `Windows.System.Power.PowerManager` (plateforme),
`H.NotifyIcon.Wpf` (tray), `NAudio` (audio en boucle). Tests : xUnit, `Microsoft.Extensions.TimeProvider.Testing`.

**Storage**: fichiers locaux — `%AppData%\BatNotif\settings.json` (réglages + pause), `%AppData%\BatNotif\logs\` (journal)

**Testing**: xUnit sur `BatNotif.Core` (moteur, réglages) avec `FakeTimeProvider` ; scénarios manuels M1–M17 du
[quickstart](quickstart.md) pour l'intégration Windows (tray, notification, plein écran, veille).

**Target Platform**: Windows 10 build 19041+ / Windows 11, x64, PC portable

**Project Type**: desktop-app (utilitaire de zone de notification)

**Performance Goals**: alerte affichée ≤ 10 s après entrée en zone d'alerte (SC-001) ; fin d'alerte ≤ 10 s après
correction (SC-007)

**Constraints**: CPU moyen < 1 % au repos (SC-006), aucune élévation de privilèges, aucun vol de focus hors plein
écran, fonctionne hors ligne, mémoire au repos visée < 100 Mo

**Scale/Scope**: 1 utilisateur, 1 batterie ; 3 fenêtres (principale, notification normale / renforcée), 1 icône
tray, 38 exigences fonctionnelles

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution **v1.2.0** (évaluation initiale faite sur v1.1.0 ; écart technique résolu par l'amendement 1.2.0).

| Principe / section | Conformité du plan | Statut |
|---|---|---|
| **I. Rappel actif** | Règles secteur × niveau, hystérésis 2 %, debounce 2 s, alerte continue, réévaluation au démarrage / réveil → moteur `BatNotif.Core` ([data-model §3](data-model.md), [engine-contract](contracts/engine-contract.md)) | ✅ |
| **II. Discrétion** | `WS_EX_NOACTIVATE` hors plein écran, mode renforcé en plein écran (exception prévue par la constitution), tray, mutex, aucun admin (HKCU), fermer ≠ quitter, polling 5 s | ✅ |
| **III. Contrôle utilisateur** | Seuils / sons / volume réglables et validés, pause 15 min / 1 h / indéfinie persistée et visible, repli sur défauts si réglages corrompus ([settings-schema](contracts/settings-schema.md)) | ✅ |
| **IV. Moteur testable** | `BatNotif.Core` en `net10.0` (aucune référence Windows possible), `TimeProvider` + `FakeTimeProvider`, tests xUnit de chaque état / transition / cas limite | ✅ |
| **V. Robustesse & observabilité** | Cas limites du §9 couverts (aucune batterie, son manquant, veille, multi-écrans, 100 % branché), journal fichier `%AppData%\BatNotif\logs`, script de publication avec étapes `[n/N]` | ✅ |
| **VI. Simplicité** | 2 dépendances applicatives justifiées ([research.md](research.md), tableau final), pas d'installeur, sons et icônes générés par le code, logger maison minimal | ✅ |
| **Contraintes techniques** | WPF, .NET 10, `PowerManager`, H.NotifyIcon, NAudio, System.Text.Json, HKCU, xUnit, `.exe` autonome : conformes à la constitution **v1.2.0** (amendée suite à ce plan : .NET 8 → .NET 10, `Battery` → `PowerManager`, R2/R3) | ✅ |
| **Indépendance** (`idee.md` §10) | Rien n'est repris d'un autre projet ; tout est créé dans ce dépôt | ✅ |
| **Workflow** | Plan soumis à validation avant `/speckit-tasks` ; `idee.md` §8 / §11.8 et README à mettre à jour | ✅ |

**Résultat du gate** : ✅ passe. L'écart initial (.NET 8 → .NET 10, API batterie) a été validé par l'utilisateur
et intégré à la constitution v1.2.0 ; il n'affecte aucun principe.

**Re-check après Phase 1** : data-model, contrats et quickstart ne créent aucun nouvel écart. ✅

## Project Structure

### Documentation (this feature)

```text
specs/001-batnotif-mvp/
├── plan.md              # Ce fichier
├── research.md          # Phase 0 : décisions techniques
├── data-model.md        # Phase 1 : entités, machine à états
├── quickstart.md        # Phase 1 : guide de validation
├── contracts/
│   ├── engine-contract.md   # Frontière moteur ↔ application
│   ├── settings-schema.md   # Format de settings.json
│   └── ui-contract.md       # Tray, raccourci, fenêtres, notification
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks — non créé ici)
```

### Source Code (repository root)

```text
BatNotif.sln
Directory.Build.props            # Réglages communs (Nullable, warnings as errors, LangVersion)
src/
├── BatNotif.Core/               # net10.0 — aucune dépendance Windows
│   ├── Battery/                 # BatterySnapshot, IBatterySource, debounce secteur
│   ├── Alerts/                  # AlertEngine (machine à états), AlertState, AlertOutput
│   ├── Settings/                # Settings, PauseState, validation, SettingsStore (JSON)
│   └── Logging/                 # LogEvent, IEventLog
└── BatNotif.App/                # net10.0-windows10.0.19041.0 — WPF
    ├── App.xaml(.cs)            # Démarrage, instance unique, câblage
    ├── Platform/                # WindowsBatterySource (PowerManager), veille, plein écran,
    │                            #   raccourci global, démarrage auto (HKCU), premier plan
    ├── Audio/                   # Lecture en boucle NAudio, sons intégrés générés
    ├── Tray/                    # Icône H.NotifyIcon, icônes générées, menu
    ├── Views/                   # MainWindow, NotificationWindow (normal / renforcé)
    ├── ViewModels/              # MainViewModel, NotificationViewModel
    └── Logging/                 # Journal fichier (rotation 14 jours)
tests/
└── BatNotif.Core.Tests/         # xUnit + FakeTimeProvider
    ├── Alerts/                  # États, transitions, hystérésis, snooze, pause, veille, aucune batterie
    ├── Battery/                 # Debounce secteur
    └── Settings/                # Validation, lecture / écriture, fichier corrompu
scripts/
└── publish.ps1                  # Publication .exe autonome, étapes [n/N]
```

**Structure Decision** : un projet moteur pur (`BatNotif.Core`) + un projet WPF (`BatNotif.App`) + un projet de
tests. La séparation Core / App est le minimum pour satisfaire le principe IV : le compilateur empêche le moteur
de dépendre de Windows. Pas d'autre couche.

## Complexity Tracking

Aucune violation à justifier.
