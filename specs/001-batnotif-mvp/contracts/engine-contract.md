# Contrat — moteur d'alerte (`BatNotif.Core`)

Frontière entre la logique testable (`BatNotif.Core`, `net10.0`, sans Windows / WPF) et l'application
(`BatNotif.App`). Les noms sont indicatifs ; le comportement est contractuel.

## Entrées

| Entrée | Fournie par | Description |
|---|---|---|
| Source batterie | App (Windows) / tests (simulée) | Donne le `BatterySnapshot` courant et signale chaque changement ([data-model §1](../data-model.md)) |
| `TimeProvider` | App (`TimeProvider.System`) / tests (`FakeTimeProvider`) | Toute notion de temps (debounce, snooze, pause) passe par lui |
| Réglages | App | Seuils, pause persistée ([data-model §2](../data-model.md)) |
| Commandes | App (UI) | `Snooze()`, `Pause(Timed 15 min \| Timed 1 h \| Indefinite)`, `Resume()`, `UpdateThresholds(low, high)`, `NotifyResumedFromSleep()`, `Evaluate()` |

## Sorties

| Sortie | Description |
|---|---|
| `AlertOutput` courant | `{ State, ShouldPresent, Level, AcPresent, PauseUntil?, PauseIndefinite, SnoozeUntil? }` |
| Événement « sortie modifiée » | Émis à chaque changement de `AlertOutput` (l'app affiche / masque la notification, change l'icône) |
| Événement « pause modifiée » | Émis pour que l'app persiste la pause (FR-026) |
| Événements de journal | Transitions, snooze, pause, reprise, au format [LogEvent](../data-model.md) |

## Garanties (couvertes par les tests unitaires)

1. Règles d'entrée / sortie d'alerte exactement celles de [data-model §3](../data-model.md), hystérésis de 2 %.
2. Changement de `AcPresent` ignoré tant qu'il n'a pas duré 2 s (debounce).
3. `ShouldPresent` reste `true` sans limite de durée tant que l'alerte est active, hors pause et snooze.
4. `Snooze()` → `ShouldPresent = false` pendant 15 min, puis `true` si l'alerte est toujours active ;
   annulé si l'alerte se termine.
5. Pause : `ShouldPresent = false` pendant toute la pause ; à l'échéance ou sur `Resume()`, réévaluation
   immédiate.
6. `Evaluate()` au démarrage et `NotifyResumedFromSleep()` donnent immédiatement le bon état (pas d'attente).
7. `HasBattery = false` → état `NoBattery`, `ShouldPresent = false`.
8. Aucune exception ne s'échappe d'un traitement d'instantané : les erreurs sont converties en événement de
   journal `Error`.
