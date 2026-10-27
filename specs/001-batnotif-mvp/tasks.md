---

description: "Liste des tâches d'implémentation du MVP BatNotif"
---

# Tasks: BatNotif MVP — notificateur de santé de batterie

**Input**: Documents de design de `specs/001-batnotif-mvp/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: **obligatoires pour le moteur et les réglages** (constitution v1.2.0, principe IV) : tests xUnit écrits
**avant** le code correspondant, qui doivent échouer puis passer. L'intégration Windows (tray, fenêtres, audio,
plein écran, veille) est validée par les scénarios manuels M1–M17 du [quickstart](quickstart.md).

**Organization** : tâches groupées par user story pour que chacune soit implémentable et testable seule.

**Règles transverses** (s'appliquent à toutes les tâches) :
- Travailler **uniquement dans `D:\bat_notif`** (CLAUDE.md §0).
- Chaque commande lancée (`dotnet restore/build/test/run`, scripts) affiche ses **logs complets** avec des étapes
  numérotées `[n/N]`, sans jamais masquer la sortie (CLAUDE.md §5).
- Une tâche n'est terminée que si `dotnet build` passe sans warning et que `dotnet test` est vert.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance sur une tâche non terminée)
- **[Story]** : user story concernée (US1, US2, US3, US4)

## Path Conventions

- Moteur : `src/BatNotif.Core/` (`net10.0`, **aucune** référence Windows / WPF)
- Application : `src/BatNotif.App/` (`net10.0-windows10.0.19041.0`, WPF)
- Tests : `tests/BatNotif.Core.Tests/` (xUnit + `FakeTimeProvider`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose** : créer la solution, les trois projets et vérifier qu'ils compilent.

- [X] T001 Créer la solution `BatNotif.sln` à la racine et `Directory.Build.props` (Nullable `enable`, ImplicitUsings `enable`, LangVersion `latest`, TreatWarningsAsErrors `true`, Company/Product `BatNotif`, Version `0.1.0`) dans `Directory.Build.props`
- [X] T002 [P] Créer le projet bibliothèque `src/BatNotif.Core/BatNotif.Core.csproj` ciblant `net10.0`, sans aucune référence de paquet ni de framework Windows
- [X] T003 [P] Créer le projet WPF `src/BatNotif.App/BatNotif.App.csproj` (`OutputType` WinExe, `UseWPF` true, TFM `net10.0-windows10.0.19041.0`, `ApplicationManifest` app.manifest, AssemblyName `BatNotif`), référence de projet vers BatNotif.Core, paquets `H.NotifyIcon.Wpf` et `NAudio` (versions stables exactes, épinglées dans le csproj et notées dans `specs/001-batnotif-mvp/research.md`, tableau des dépendances) ; créer `src/BatNotif.App/app.manifest` (`asInvoker`, DPI `PerMonitorV2`, compatibilité Windows 10/11)
- [X] T004 [P] Créer le projet de tests `tests/BatNotif.Core.Tests/BatNotif.Core.Tests.csproj` (`net10.0`, xUnit, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Microsoft.Extensions.TimeProvider.Testing`) avec référence vers BatNotif.Core
- [X] T005 [P] Créer `.editorconfig` à la racine (indentation 4 espaces, fins de ligne CRLF, conventions de nommage C# standard, `file_scoped` namespaces)
- [X] T006 Ajouter les trois projets à `BatNotif.sln`, puis lancer `dotnet restore BatNotif.sln -v normal` et `dotnet build BatNotif.sln -v normal` avec logs `[1/2]`, `[2/2]` et vérifier un build sans erreur ni warning

**Checkpoint** : la solution compile, `dotnet test` s'exécute (0 test).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose** : types du moteur et briques d'infrastructure utilisés par toutes les stories.

**⚠️ CRITICAL** : aucune story ne peut commencer avant la fin de cette phase.

- [X] T007 [P] Créer le record `BatterySnapshot` (`Level` 0–100, `HasBattery`, `AcPresent`, `Charging`, `Discharging`, `ObservedAt`) avec la propriété dérivée `EffectivelyOnBattery = !AcPresent || Discharging`, selon data-model §1, dans `src/BatNotif.Core/Battery/BatterySnapshot.cs`
- [X] T008 [P] Créer l'interface `IBatterySource` (propriété `Current` de type `BatterySnapshot`, événement `SnapshotChanged`) dans `src/BatNotif.Core/Battery/IBatterySource.cs`
- [X] T009 [P] Créer `LogEventKind` (AppStart, AppStop, StateChanged, AlertPresented, AlertPresentFailed, Snooze, PauseStarted, PauseEnded, SettingsSaved, SettingsReset, SoundFallback, HotkeyUnavailable, Error), le record `LogEvent` (`Timestamp`, `Kind`, `Detail`, méthode de formatage `<Timestamp> [<Kind>] <Detail>`) et l'interface `IEventLog` (`Write(LogEvent)`) dans `src/BatNotif.Core/Logging/LogEvent.cs` et `src/BatNotif.Core/Logging/IEventLog.cs`
- [X] T010 [P] Créer `AlertConstants` (hystérésis 2 %, snooze 15 min, debounce secteur 2 s, polling 5 s, seuils par défaut 20 / 80, bornes 5–95, écart minimum 5) dans `src/BatNotif.Core/Alerts/AlertConstants.cs`
- [X] T011 [P] Créer l'énumération `AlertState` (Normal, AlertLow, AlertHigh, NoBattery) et le record `AlertOutput` (`State`, `ShouldPresent`, `Level`, `AcPresent`, `PauseUntil`, `PauseIndefinite`, `SnoozeUntil`) selon data-model §3 et engine-contract, dans `src/BatNotif.Core/Alerts/AlertState.cs` et `src/BatNotif.Core/Alerts/AlertOutput.cs`
- [X] T012 [P] Créer `PauseKind` (None, Timed, Indefinite), le record `PauseState` (`Kind`, `Until` UTC) et la classe `Settings` (LowThreshold, HighThreshold, LowSoundPath, HighSoundPath, Volume, StartWithWindows, Pause) avec valeurs par défaut de data-model §2, dans `src/BatNotif.Core/Settings/PauseState.cs` et `src/BatNotif.Core/Settings/Settings.cs`
- [X] T013 [P] Créer les doublures de test `FakeBatterySource` (permet de pousser un `BatterySnapshot` et lève `SnapshotChanged`) et `InMemoryEventLog` (liste des `LogEvent` écrits) dans `tests/BatNotif.Core.Tests/Fakes/FakeBatterySource.cs` et `tests/BatNotif.Core.Tests/Fakes/InMemoryEventLog.cs`
- [X] T014 [P] Implémenter `FileEventLog : IEventLog` (fichier `%AppData%\BatNotif\logs\batnotif-<yyyy-MM-dd>.log`, une ligne par événement, écriture thread-safe, création du dossier, suppression des fichiers de plus de 14 jours au démarrage, ne lève jamais d'exception) dans `src/BatNotif.App/Logging/FileEventLog.cs`
- [X] T015 Créer `App.xaml` / `App.xaml.cs` (ShutdownMode `OnExplicitShutdown`, pas de StartupUri), avec des gestionnaires globaux `DispatcherUnhandledException`, `AppDomain.CurrentDomain.UnhandledException` et `TaskScheduler.UnobservedTaskException` qui journalisent un `LogEvent` Error sans fermer l'application (FR-035) et lèvent un événement statique `App.CriticalErrorReported` (message en français) quand l'erreur peut empêcher les alertes, et journalisation `AppStart` / `AppStop`, dans `src/BatNotif.App/App.xaml` et `src/BatNotif.App/App.xaml.cs`
- [X] T016 Implémenter `WindowsBatterySource : IBatterySource` avec `Windows.System.Power.PowerManager` (`RemainingChargePercent`, `BatteryStatus`, `PowerSupplyStatus` et leurs événements) plus un polling de secours toutes les 5 s via `TimeProvider` ; `BatteryStatus.NotPresent` → `HasBattery = false` ; `Discharging` vrai même sur secteur ; `SnapshotChanged` levé uniquement si l'instantané change (research R3), dans `src/BatNotif.App/Platform/WindowsBatterySource.cs`
- [X] T017 Implémenter `SingleInstance` (`Mutex` nommé `Local\BatNotif.SingleInstance` + `EventWaitHandle` nommé ; deuxième lancement → signale l'instance existante et quitte ; l'instance existante lève un événement `ActivationRequested`, qui affichera la fenêtre principale dès qu'elle existe (US2) et est journalisé en attendant) et l'appeler **en tout premier** dans `App.OnStartup` (constitution principe II, FR-032), dans `src/BatNotif.App/Platform/SingleInstance.cs` et `src/BatNotif.App/App.xaml.cs`

**Checkpoint** : types du moteur, journal, source batterie Windows et instance unique prêts.

---

## Phase 3: User Story 1 - Être alerté et relancé quand la batterie sort de sa zone de confort (Priority: P1) 🎯 MVP

**Goal** : avec les réglages par défaut (20 % / 80 %), afficher une notification maison persistante avec un son en
boucle quand la batterie est trop basse sur batterie ou trop haute sur secteur, jusqu'à correction ; alerte
renforcée en plein écran ; évaluation immédiate au démarrage et au réveil.

**Independent Test** : lancer l'app en Debug avec `--high N` (N = niveau actuel − 1, chargeur branché) ou
`--low N` (N = niveau actuel + 1, sur batterie) → notification + son en boucle, sans perte de focus ; corriger la
situation → tout s'arrête en ≤ 10 s. Aucun code n'est modifié pour tester. Scénarios M1, M2, M3, M7, M8, M15 du
quickstart.

### Tests for User Story 1 ⚠️ (écrire d'abord, vérifier qu'ils échouent)

- [X] T018 [P] [US1] Écrire les tests du debounce secteur avec `FakeTimeProvider` : changement de secteur ignoré s'il dure < 2 s, pris en compte après 2 s, micro-coupures répétées ignorées, dans `tests/BatNotif.Core.Tests/Battery/AcPowerDebouncerTests.cs`
- [X] T019 [P] [US1] Écrire les tests des transitions du moteur : Normal→AlertLow (sur batterie ≤ 20 %, et sur secteur avec `Discharging`), Normal→AlertHigh (secteur ≥ 80 %), fin AlertLow (≥ 22 % ou secteur + charge), fin AlertHigh (≤ 78 % ou débranché), aucune alerte à 82 % sur batterie ni bas sur secteur en charge (FR-009), oscillation 78↔79 % sans nouvelle alerte (hystérésis), `HasBattery = false` → NoBattery sans présentation, dans `tests/BatNotif.Core.Tests/Alerts/AlertEngineTransitionTests.cs`
- [X] T020 [P] [US1] Écrire les tests de continuité et d'évaluation : `ShouldPresent` reste vrai pendant 6 h simulées tant que la condition dure (FR-010), `Evaluate()` au démarrage donne immédiatement AlertHigh si branché à 90 % et Normal si sur batterie à 90 % (FR-004), `NotifyResumedFromSleep()` réévalue sans attendre, `OutputChanged` levé à chaque changement uniquement, événements `StateChanged` journalisés, une exception de la source est convertie en `LogEvent` Error sans propagation, dans `tests/BatNotif.Core.Tests/Alerts/AlertEngineContinuityTests.cs`

### Implementation for User Story 1

- [X] T021 [US1] Implémenter `AcPowerDebouncer` (filtre de 2 s sur `AcPresent`, basé sur `TimeProvider`) pour faire passer T018, dans `src/BatNotif.Core/Battery/AcPowerDebouncer.cs`
- [X] T022 [US1] Implémenter `AlertEngine` (constructeur : `IBatterySource`, `TimeProvider`, `IEventLog`, seuils ; machine à états de data-model §3 ; `Evaluate()`, `NotifyResumedFromSleep()`, propriété `Output`, événement `OutputChanged` ; journalisation des transitions ; aucune exception ne s'échappe) pour faire passer T019 et T020, dans `src/BatNotif.Core/Alerts/AlertEngine.cs`
- [X] T023 [P] [US1] Implémenter `SleepResumeWatcher` (abonnement `SystemEvents.PowerModeChanged` → `Resume`, plus détection d'un saut d'horloge > 30 s entre deux ticks de 5 s ; lève un événement `Resumed`) dans `src/BatNotif.App/Platform/SleepResumeWatcher.cs`
- [X] T024 [P] [US1] Créer `NativeMethods` (P/Invoke : `GetWindowLong`/`SetWindowLong` pour `WS_EX_NOACTIVATE` et `WS_EX_TOOLWINDOW`, `GetForegroundWindow`, `MonitorFromWindow`, `GetMonitorInfo`, `GetWindowRect`, `SHQueryUserNotificationState`, `SetForegroundWindow`, `keybd_event`, `RegisterHotKey`, `UnregisterHotKey`) dans `src/BatNotif.App/Platform/NativeMethods.cs`
- [X] T025 [P] [US1] Implémenter `BuiltInSounds` : génération en mémoire de deux motifs sonores distincts et reconnaissables (alerte basse : bips graves descendants ; alerte haute : bips aigus montants) avec les générateurs de signal NAudio, utilisables en boucle (research R6), dans `src/BatNotif.App/Audio/BuiltInSounds.cs`
- [X] T026 [US1] Implémenter `LoopingAlarmPlayer` (lecture en boucle sans coupure d'un fichier MP3/WAV via `AudioFileReader` + `WaveOutEvent`, volume 0–1, `Play(alertType, path)`, `Stop()` ; fichier vide → son intégré ; fichier introuvable ou illisible → son intégré + événement `SoundFallback` journalisé (FR-015, FR-016) ; `PlayPreview(path, volume)` joue une seule fois) dans `src/BatNotif.App/Audio/LoopingAlarmPlayer.cs`
- [X] T027 [P] [US1] Implémenter `FullscreenDetector` (`IsFullscreenActive()` : vrai si `SHQueryUserNotificationState` renvoie D3D plein écran, busy ou présentation, ou si la fenêtre au premier plan couvre tout son écran ; ignore le bureau et la barre des tâches) dans `src/BatNotif.App/Platform/FullscreenDetector.cs`
- [X] T028 [P] [US1] Implémenter `ForegroundHelper.ForceForeground(Window)` (simulation d'une pression Alt via `keybd_event`, puis `SetForegroundWindow` ; renvoie le succès) et `ScreenHelper` (zone de travail de l'écran qui contient la fenêtre au premier plan) dans `src/BatNotif.App/Platform/ForegroundHelper.cs` et `src/BatNotif.App/Platform/ScreenHelper.cs`
- [X] T029 [P] [US1] Créer `NotificationViewModel` (type d'alerte, niveau mis à jour en direct, message « Batterie à X % — branche le chargeur » / « — débranche le chargeur », mode Normal / Renforcé ; INotifyPropertyChanged) dans `src/BatNotif.App/ViewModels/NotificationViewModel.cs`
- [X] T030 [US1] Créer `NotificationWindow` (WindowStyle None, fond arrondi, Topmost, ShowActivated false, ShowInTaskbar false, styles `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` appliqués dans `SourceInitialized` ; icône + message + niveau ; mode normal ≈ 360×120 en haut à droite de l'écran actif ; mode renforcé ≈ 2× plus grand, centré, sans `WS_EX_NOACTIVATE` ; méthode `SetMode(mode)` ; aucun bouton de fermeture, pas de fermeture automatique) selon ui-contract §4, dans `src/BatNotif.App/Views/NotificationWindow.xaml` et `src/BatNotif.App/Views/NotificationWindow.xaml.cs`
- [X] T031 [US1] Implémenter `AlertPresenter` : sur `OutputChanged`, si `ShouldPresent` → affiche la notification et lance le son du type d'alerte, sinon → masque et coupe ; vérifie le plein écran toutes les 5 s pendant une alerte et bascule le mode (renforcé : `ForceForeground`, échec → `AlertPresentFailed` journalisé, la fenêtre reste Topmost et le son continue) ; journalise `AlertPresented` (FR-011, FR-037) ; tout sur le Dispatcher UI, dans `src/BatNotif.App/Presentation/AlertPresenter.cs`
- [X] T032 [US1] Créer `TrayIconService` minimal (icône H.NotifyIcon statique générée par le code, info-bulle « BatNotif », menu contextuel avec une seule entrée « Quitter » qui arrête proprement l'application) dans `src/BatNotif.App/Tray/TrayIconService.cs`
- [X] T033 [US1] Câbler le démarrage dans `App.OnStartup` : création de `FileEventLog`, `WindowsBatterySource`, `AlertEngine` (seuils par défaut `Settings`), `LoopingAlarmPlayer`, `AlertPresenter`, `SleepResumeWatcher` (→ `NotifyResumedFromSleep()`), `TrayIconService`, puis `engine.Evaluate()` immédiat (FR-004) ; libération propre à l'arrêt ; **en build Debug uniquement** (`#if DEBUG`), lecture des arguments `--low N` / `--high N` qui remplacent les seuils pour la session sans les enregistrer, avec un `LogEvent` indiquant le remplacement, dans `src/BatNotif.App/App.xaml.cs`
- [X] T034 [US1] Vérifier US1 : `dotnet build` + `dotnet test` (logs complets), lancer l'app (`dotnet run --project src/BatNotif.App -- --high N`) et exécuter M1, M2, M3, M7, M8, M15 du quickstart ; consigner les résultats (OK / KO + observations) dans `specs/001-batnotif-mvp/checklists/manual-validation.md` ; mettre à jour l'état d'avancement de `README.md`

**Checkpoint** : MVP fonctionnel. Alertes continues avec réglages par défaut, plein écran renforcé, démarrage et réveil.

---

## Phase 4: User Story 2 - Régler les seuils, les sons et le volume (Priority: P2)

**Goal** : fenêtre principale lisible en un coup d'œil, avec réglages validés, persistés et appliqués : seuils,
sons par type, volume, bouton Tester, démarrage avec Windows.

**Independent Test** : modifier chaque réglage, Enregistrer, quitter, relancer → tout est restauré et appliqué
aux alertes suivantes. Scénarios M9, M10, M11, M12, M16.

### Tests for User Story 2 ⚠️ (écrire d'abord, vérifier qu'ils échouent)

- [X] T035 [P] [US2] Écrire les tests de validation : seuils hors 5–95 refusés, `HighThreshold − LowThreshold < 5` refusé avec message de contrainte explicite, volume borné 0–1, défauts valides, dans `tests/BatNotif.Core.Tests/Settings/SettingsValidatorTests.cs`
- [X] T036 [P] [US2] Écrire les tests de persistance (dossier temporaire) : fichier absent → défauts ; aller-retour complet identique ; JSON illisible → fichier renommé `settings.corrupt-<horodatage>.json` + défauts + `SettingsReset` ; clé inconnue ignorée ; clé manquante → défaut ; valeur hors bornes → défaut du champ + avertissement ; pause `Timed` expirée → `None` ; écriture atomique via `.tmp` ; format conforme à contracts/settings-schema.md (camelCase, `schemaVersion` 1), dans `tests/BatNotif.Core.Tests/Settings/SettingsStoreTests.cs`
- [X] T037 [P] [US2] Écrire les tests de mise à jour des seuils à chaud : `UpdateThresholds(low, high)` réévalue immédiatement (entrée ou sortie d'alerte selon les nouveaux seuils), dans `tests/BatNotif.Core.Tests/Alerts/AlertEngineThresholdTests.cs`

### Implementation for User Story 2

- [X] T038 [P] [US2] Implémenter `SettingsValidator` (résultat avec liste de messages en français) pour faire passer T035, dans `src/BatNotif.Core/Settings/SettingsValidator.cs`
- [X] T039 [US2] Implémenter `SettingsStore` (`Load()` → `Settings` + liste d'avertissements, `Save(Settings)` atomique ; `System.Text.Json` camelCase indenté ; chemin injecté, par défaut `%AppData%\BatNotif\settings.json` fourni par l'App ; journalise `SettingsReset` / `SettingsSaved`) pour faire passer T036, dans `src/BatNotif.Core/Settings/SettingsStore.cs`
- [X] T040 [US2] Ajouter `UpdateThresholds(low, high)` à `AlertEngine` pour faire passer T037, dans `src/BatNotif.Core/Alerts/AlertEngine.cs`
- [X] T041 [P] [US2] Implémenter `AutostartRegistry` (`IsEnabled()`, `Enable()`, `Disable()` sur `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, valeur `BatNotif` = chemin de l'exe courant entre guillemets ; erreurs journalisées, jamais levées) dans `src/BatNotif.App/Platform/AutostartRegistry.cs`
- [X] T042 [US2] Créer `MainViewModel` (niveau, secteur, libellé d'état, seuils, chemins de sons, volume, démarrage avec Windows, liste d'avertissements ; commandes Enregistrer (valide → sauvegarde → `UpdateThresholds` → met à jour le lecteur audio → synchronise `AutostartRegistry`), Réinitialiser seuil bas / haut, Choisir un son (sélecteur `*.mp3;*.wav`), Tester un son (`PlayPreview`) ; mise à jour en direct depuis `OutputChanged` ; avertissements alimentés par : les avertissements de `SettingsStore.Load()`, l'événement `SoundFallback` du lecteur audio et une vérification d'existence des fichiers son au chargement et à l'enregistrement (FR-016), l'événement `App.CriticalErrorReported` (FR-035), et `HotkeyUnavailable` (US4)) dans `src/BatNotif.App/ViewModels/MainViewModel.cs`
- [X] T043 [US2] Créer `MainWindow` (zones État, Alerte basse, Alerte haute, Volume, Démarrer avec Windows, Actions, Avertissements selon ui-contract §3 ; messages de validation affichés ; la fermeture ✕ masque la fenêtre au lieu de quitter (FR-030)) dans `src/BatNotif.App/Views/MainWindow.xaml` et `src/BatNotif.App/Views/MainWindow.xaml.cs`
- [X] T044 [US2] Câbler les réglages au démarrage dans `App.OnStartup` : `SettingsStore.Load()` avant la création du moteur (seuils, sons, volume), avertissements transmis au `MainViewModel`, `MainWindow` créée masquée, entrée « Ouvrir » ajoutée au menu tray pour l'afficher, `SingleInstance.ActivationRequested` relié à l'affichage de `MainWindow`, dans `src/BatNotif.App/App.xaml.cs` et `src/BatNotif.App/Tray/TrayIconService.cs`
- [X] T045 [US2] Vérifier US2 : build + tests (logs complets), exécuter M9, M10, M11, M12, M16 du quickstart, puis **chronométrer SC-005** (régler les deux seuils, choisir et tester les deux sons en partant d'une installation neuve : objectif < 2 min) ; consigner les résultats dans `specs/001-batnotif-mvp/checklists/manual-validation.md` ; mettre à jour l'état d'avancement de `README.md`

**Checkpoint** : US1 et US2 fonctionnent ; les réglages survivent au redémarrage.

---

## Phase 5: User Story 3 - Faire taire l'alerte temporairement (Priority: P2)

**Goal** : Snooze 15 min depuis la notification ; Pause 15 min / 1 h / indéfinie depuis la fenêtre et le tray ;
Reprendre ; pause persistée et visible.

**Independent Test** : déclencher une alerte, Snooze → silence 15 min puis retour ; Pause 1 h, redémarrer → pause
toujours active et visible. Scénarios M4, M5, M6.

### Tests for User Story 3 ⚠️ (écrire d'abord, vérifier qu'ils échouent)

- [X] T046 [P] [US3] Écrire les tests du snooze avec `FakeTimeProvider` : `Snooze()` → `ShouldPresent` faux pendant 15 min puis vrai si l'alerte dure ; snooze annulé si l'alerte se termine pendant le snooze ; `Snooze()` sans effet en Normal ou NoBattery ; `SnoozeUntil` exposé ; `LogEvent` Snooze journalisé, dans `tests/BatNotif.Core.Tests/Alerts/AlertEngineSnoozeTests.cs`
- [X] T047 [P] [US3] Écrire les tests de la pause : Timed 15 min et 1 h, Indefinite ; aucune présentation pendant la pause alors que l'état réel continue d'être calculé (FR-024) ; échéance → réévaluation immédiate et alerte si condition vraie ; `Resume()` → réévaluation immédiate (FR-025) ; événement `PauseChanged` levé à chaque changement ; pause restaurée au démarrage, pause expirée ignorée ; `PauseStarted` / `PauseEnded` journalisés, dans `tests/BatNotif.Core.Tests/Alerts/AlertEnginePauseTests.cs`

### Implementation for User Story 3

- [X] T048 [US3] Ajouter à `AlertEngine` `Snooze()`, `Pause(PauseState)`, `Resume()`, l'état de pause initial passé au constructeur, l'événement `PauseChanged` et les minuteries via `TimeProvider.CreateTimer` pour faire passer T046 et T047, dans `src/BatNotif.Core/Alerts/AlertEngine.cs`
- [X] T049 [US3] Ajouter le bouton unique « Snooze 15 min » à la notification (appelle `engine.Snooze()`, utilisable sans prendre le focus en mode normal) selon ui-contract §4, dans `src/BatNotif.App/Views/NotificationWindow.xaml`, `src/BatNotif.App/Views/NotificationWindow.xaml.cs` et `src/BatNotif.App/ViewModels/NotificationViewModel.cs`
- [X] T050 [US3] Persister la pause : sur `PauseChanged`, enregistrer `Settings.Pause` via `SettingsStore.Save` ; au démarrage, passer la pause chargée au moteur, dans `src/BatNotif.App/App.xaml.cs`
- [X] T051 [P] [US3] Ajouter à `MainViewModel` / `MainWindow` le bouton Pause avec choix 15 min / 1 h / indéfiniment, le bouton Reprendre quand une pause est active, et les libellés d'état « Pause jusqu'à HH:MM », « Pause indéfinie », « Snooze jusqu'à HH:MM » (FR-033), dans `src/BatNotif.App/ViewModels/MainViewModel.cs` et `src/BatNotif.App/Views/MainWindow.xaml`
- [X] T052 [P] [US3] Ajouter au menu tray les entrées Pause 15 min / Pause 1 h / Pause indéfinie (visibles hors pause) et Reprendre (visible en pause) selon ui-contract §1, dans `src/BatNotif.App/Tray/TrayIconService.cs`
- [X] T053 [US3] Vérifier US3 : build + tests (logs complets), exécuter M4, M5, M6 du quickstart et consigner les résultats dans `specs/001-batnotif-mvp/checklists/manual-validation.md` ; mettre à jour l'état d'avancement de `README.md` (**première version livrable** : US1 + US2 + US3)

**Checkpoint** : US1, US2 et US3 fonctionnent indépendamment.

---

## Phase 6: User Story 4 - Présence discrète et toujours accessible (Priority: P3)

**Goal** : icône tray qui reflète l'état, clic gauche / droit, raccourci global `Ctrl+Alt+B`, « Quitter » qui
ferme tout (l'instance unique est déjà en place depuis la phase 2).

**Independent Test** : fermer la fenêtre → alertes toujours actives ; `Ctrl+Alt+B` la rouvre ; deuxième lancement
→ pas de doublon. Scénarios M13, M14.

### Implementation for User Story 4

- [ ] T054 [P] [US4] Implémenter `TrayIconFactory` : icônes générées par le code (pastille de couleur + niveau) pour les états normal, alerte, pause et aucune batterie (FR-028), dans `src/BatNotif.App/Tray/TrayIconFactory.cs`
- [ ] T055 [US4] Compléter `TrayIconService` : icône et info-bulle `BatNotif — 74 % · branché · normal` mises à jour sur `OutputChanged` ; clic gauche → affiche / masque `MainWindow` ; menu final Ouvrir / Pause… / Reprendre / Quitter (FR-029), dans `src/BatNotif.App/Tray/TrayIconService.cs`
- [ ] T056 [P] [US4] Implémenter `GlobalHotkey` (`RegisterHotKey` `Ctrl+Alt+B` sur une `HwndSource` de messages dédiée ; échec → `HotkeyUnavailable` journalisé et avertissement dans la fenêtre principale) (FR-031), dans `src/BatNotif.App/Platform/GlobalHotkey.cs`
- [ ] T057 [US4] Câbler dans `App.OnStartup` : enregistrement de `GlobalHotkey` → afficher `MainWindow`, « Quitter » → arrêt complet (moteur, audio, tray, hotkey, mutex libérés, `AppStop` journalisé), dans `src/BatNotif.App/App.xaml.cs`
- [ ] T058 [US4] Vérifier US4 : build + tests (logs complets), exécuter M13, M14 du quickstart et consigner les résultats dans `specs/001-batnotif-mvp/checklists/manual-validation.md` ; mettre à jour l'état d'avancement de `README.md`

**Checkpoint** : toutes les user stories fonctionnent.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose** : publication, performance, journal, documentation.

- [ ] T059 Ajouter les propriétés de publication à `src/BatNotif.App/BatNotif.App.csproj` (`RuntimeIdentifier` win-x64, `SelfContained` true, `PublishSingleFile` true, `EnableCompressionInSingleFile` true, `IncludeNativeLibrariesForSelfExtract` true) et créer `scripts/publish.ps1` (étapes `[1/4]` nettoyage de `publish/`, `[2/4]` tests, `[3/4]` `dotnet publish` en Release avec logs normaux, `[4/4]` affichage du chemin et de la taille de `publish/BatNotif.exe` ; arrêt avec message clair si une étape échoue), dans `scripts/publish.ps1`
- [ ] T060 Exécuter `scripts/publish.ps1`, puis lancer `publish/BatNotif.exe` et vérifier qu'il démarre sans droits admin (scénario quickstart §4) ; consigner le résultat dans `specs/001-batnotif-mvp/checklists/manual-validation.md`
- [ ] T061 Mesurer la consommation au repos pendant 10 min (CPU moyen et mémoire de `BatNotif` via `Get-Process`, échantillon toutes les 10 s, sortie affichée) et vérifier SC-006 (CPU < 1 %, mémoire < 100 Mo ; l'impact sur l'autonomie est considéré couvert par un CPU moyen < 1 %, pas de mesure d'autonomie séparée) ; consigner les mesures dans `specs/001-batnotif-mvp/checklists/manual-validation.md`
- [ ] T062 Vérifier le journal après les scénarios (M17) : présence de AppStart, StateChanged, AlertPresented, Snooze, PauseStarted / PauseEnded, SettingsSaved ; consigner dans `specs/001-batnotif-mvp/checklists/manual-validation.md`
- [ ] T063 [P] Mettre à jour `README.md` : état d'avancement (implémentation terminée), section « Compiler et lancer » (prérequis SDK .NET 10, `dotnet build`, `dotnet test`, `dotnet run --project src/BatNotif.App`, `scripts/publish.ps1`), emplacement des réglages et du journal, et note sur « Ne pas déranger / Focus Assist » (la notification s'affiche quand même, le son reste soumis au volume et au mixeur Windows)
- [ ] T064 [P] Reporter dans `idee.md` toute décision ou tout écart apparu pendant l'implémentation (ou confirmer qu'il n'y en a aucun), et aligner la roadmap §12 sur l'état réel
- [ ] T065 Passe finale : `dotnet build BatNotif.sln -c Release -v normal` sans warning et `dotnet test` entièrement vert (logs complets), relecture des TODO restants dans `src/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance.
- **Foundational (Phase 2)** : dépend de la Phase 1. **Bloque toutes les stories.**
- **US1 (Phase 3)** : dépend de la Phase 2. C'est le MVP.
- **US2 (Phase 4)** : dépend de la Phase 2. Utilise le moteur et le lecteur audio de US1 (T022, T026) et étend le
  menu tray de T032.
- **US3 (Phase 5)** : dépend de US1 (moteur, notification). Les libellés de fenêtre (T051) dépendent de US2 (T042,
  T043) ; la persistance (T050) dépend de T039.
- **US4 (Phase 6)** : dépend de US1 (tray minimal T032). Le clic gauche et le raccourci affichent `MainWindow` (US2,
  T043). L'instance unique est en phase 2 (T017) pour que chaque incrément respecte la constitution (principe II).
- **Polish (Phase 7)** : dépend de toutes les stories visées.

### User Story Dependencies

```text
Setup → Foundational → US1 (MVP) ─┬─→ US2 ─┬─→ US3
                                  │        └─→ US4
                                  └─────────→ (US3 / US4 utilisent aussi US1)
                                            → Polish
```

Ordre recommandé (un seul développeur) : **US1 → US2 → US3 → US4 → Polish**.

### Within Each User Story

- Tests du moteur / des réglages écrits **avant** le code, vérifiés **en échec**, puis passés au vert.
- Types du moteur → logique du moteur → services de plateforme → vues → câblage dans `App.xaml.cs` → vérification
  manuelle.
- Chaque story se termine par sa tâche de vérification (build + tests + scénarios du quickstart).

### Parallel Opportunities

- Phase 1 : T002, T003, T004, T005 en parallèle après T001.
- Phase 2 : T007 à T014 en parallèle ; T015, T016 et T017 ensuite.
- US1 : tests T018–T020 en parallèle ; puis T023, T024, T025, T027, T028, T029 en parallèle pendant que T021 → T022
  avancent.
- US2 : tests T035–T037 en parallèle ; T038 et T041 en parallèle.
- US3 : tests T046–T047 en parallèle ; T051 et T052 en parallèle.
- US4 : T054 et T056 en parallèle.
- Polish : T063 et T064 en parallèle.

---

## Parallel Example: User Story 1

```text
# Tests du moteur (écrits en premier, en parallèle) :
Task: "T018 Tests du debounce secteur dans tests/BatNotif.Core.Tests/Battery/AcPowerDebouncerTests.cs"
Task: "T019 Tests des transitions dans tests/BatNotif.Core.Tests/Alerts/AlertEngineTransitionTests.cs"
Task: "T020 Tests de continuité dans tests/BatNotif.Core.Tests/Alerts/AlertEngineContinuityTests.cs"

# Services de plateforme (en parallèle, fichiers distincts) :
Task: "T023 SleepResumeWatcher dans src/BatNotif.App/Platform/SleepResumeWatcher.cs"
Task: "T024 NativeMethods dans src/BatNotif.App/Platform/NativeMethods.cs"
Task: "T025 BuiltInSounds dans src/BatNotif.App/Audio/BuiltInSounds.cs"
Task: "T027 FullscreenDetector dans src/BatNotif.App/Platform/FullscreenDetector.cs"
```

---

## Implementation Strategy

### Jalon interne puis première version livrable

1. Phase 1 : Setup.
2. Phase 2 : Foundational (bloquante, inclut l'instance unique).
3. Phase 3 : US1 → **STOP et VALIDATION** (T034). C'est un **jalon interne** : le cœur de l'alerte fonctionne, mais
   l'app ne respecte pas encore toute la constitution (pas de réglages persistés ni de pause, principe III). Elle
   n'est pas distribuée à ce stade.
4. Phases 4 et 5 : US2 et US3 → **première version livrable**, conforme à la constitution.

### Incremental Delivery

1. Setup + Foundational → fondations prêtes.
2. US1 → validation → commit (jalon interne : alertes continues avec seuils par défaut).
3. US2 → validation → commit (réglages personnalisés et persistés).
4. US3 → validation → commit (snooze et pause → **première version livrable**).
5. US4 → validation → commit (tray complet, raccourci).
6. Polish → publication de l'exe → commit.

Chaque incrément s'ajoute sans casser les précédents. Le README est mis à jour à chaque étape.

---

## Notes

- [P] = fichiers différents, aucune dépendance sur une tâche non terminée.
- [USx] = traçabilité vers la user story de la spec.
- Les tests du moteur doivent échouer avant l'implémentation (principe IV).
- Commit après chaque story validée, avec contenu et message montrés à l'utilisateur avant (règle du projet).
- Ne jamais lire ni réutiliser un fichier en dehors de `D:\bat_notif`.
