# Data Model — 001-batnotif-mvp

Entités issues de [spec.md](spec.md) (section *Key Entities*). Les types sont décrits de façon logique ;
leur forme exacte dans le code est fixée à l'implémentation.

---

## 1. BatterySnapshot — instantané observé

Produit par la source batterie (Windows en production, simulée en test), consommé par le moteur.

| Champ | Type logique | Règles |
|---|---|---|
| `Level` | entier 0–100 | Pourcentage arrondi |
| `HasBattery` | booléen | `false` → état `NoBattery` (FR-005) |
| `AcPresent` | booléen | Secteur branché (après debounce de 2 s, FR-003) |
| `Charging` | booléen | La batterie se charge réellement |
| `Discharging` | booléen | La batterie se vide réellement, même sur secteur (chargeur trop faible) |
| `ObservedAt` | horodatage | Fourni par `TimeProvider` |

Dérivé utile : **`EffectivelyOnBattery = !AcPresent || Discharging`** (règle de l'alerte basse, FR-006).

---

## 2. Settings — réglages persistés

Type C# : `AppSettings` (et non `Settings`, pour éviter le conflit avec le namespace `BatNotif.Core.Settings`).

Fichier `%AppData%\BatNotif\settings.json` — format exact dans [contracts/settings-schema.md](contracts/settings-schema.md).

| Champ | Type | Défaut | Validation |
|---|---|---|---|
| `LowThreshold` | entier | 20 | 5–95 |
| `HighThreshold` | entier | 80 | 5–95 et `HighThreshold − LowThreshold ≥ 5` (FR-018) |
| `LowSoundPath` | chemin ou vide | vide (= son intégré) | Si renseigné et introuvable → son intégré + avertissement (FR-016) |
| `HighSoundPath` | chemin ou vide | vide (= son intégré) | Idem |
| `Volume` | décimal 0.0–1.0 | 0.8 | Borné à [0, 1] |
| `StartWithWindows` | booléen | `false` | Synchronisé avec la clé `HKCU\…\Run` (FR-022) |
| `Pause` | `PauseState` | aucune | Voir §4 |

Constantes non réglables dans le MVP : hystérésis **2 %**, snooze **15 min**, debounce secteur **2 s**,
polling de secours **5 s**.

Seuils de fin dérivés : `LowReset = LowThreshold + 2`, `HighReset = HighThreshold − 2`.

---

## 3. AlertState — machine à états du moteur

États :

| État | Signification | Notification / son |
|---|---|---|
| `Normal` | Aucune condition d'alerte | Rien |
| `AlertLow` | Sur batterie (effectivement) et `Level ≤ LowThreshold` | Affichée + son bas en boucle |
| `AlertHigh` | Secteur présent et `Level ≥ HighThreshold` | Affichée + son haut en boucle |
| `NoBattery` | Aucune batterie détectée | Rien ; fenêtre + icône l'indiquent |

Modificateurs orthogonaux (s'appliquent par-dessus l'état d'alerte, qui continue d'être calculé) :

| Modificateur | Effet |
|---|---|
| `Paused(until \| indefinite)` | Aucune notification ni son, quel que soit l'état (FR-024) |
| `Snoozed(until)` | Notification masquée et son coupé pour l'alerte en cours (FR-027) |

**Sortie du moteur** = `AlertOutput` : `{ State, ShouldPresent (bool), PresentationMode (Normal | Fullscreen),
Level, PauseUntil?, SnoozeUntil? }`.
`ShouldPresent = State ∈ {AlertLow, AlertHigh} && !Paused && !Snoozed`.
Le choix `PresentationMode` est fait côté application (détection plein écran), pas dans le moteur.

### Transitions

| Depuis | Condition | Vers | Effets |
|---|---|---|---|
| `Normal` | `EffectivelyOnBattery && Level ≤ LowThreshold` | `AlertLow` | Début d'alerte, journal |
| `Normal` | `AcPresent && Level ≥ HighThreshold` | `AlertHigh` | Début d'alerte, journal |
| `AlertLow` | `Level ≥ LowReset` **ou** (`AcPresent && Charging`) | `Normal` | Fin d'alerte, snooze annulé |
| `AlertHigh` | `Level ≤ HighReset` **ou** `!AcPresent` | `Normal` | Fin d'alerte, snooze annulé |
| tout état | `!HasBattery` | `NoBattery` | Fin d'alerte éventuelle |
| `NoBattery` | `HasBattery` | réévaluation complète | — |
| `AlertLow` ↔ `AlertHigh` | (impossible directement : conditions secteur opposées) | via `Normal` | — |

Événements qui déclenchent une **réévaluation immédiate** : démarrage de l'application (FR-004), sortie de
veille (FR-004), fin de pause ou « Reprendre » (FR-025), fin de snooze (FR-027), nouvel instantané batterie,
modification des seuils.

Règles de modificateurs :
- `Snooze` n'est possible que si l'état est `AlertLow` ou `AlertHigh` ; il est annulé quand l'alerte se termine.
- `Pause` est possible dans tout état ; sa fin (échéance ou « Reprendre ») déclenche une réévaluation.
- Au démarrage, une pause dont l'échéance est passée est ignorée (et supprimée des réglages).

---

## 4. PauseState — pause persistée

| Champ | Type | Règles |
|---|---|---|
| `Kind` | `None` / `Timed` / `Indefinite` | — |
| `Until` | horodatage UTC ou vide | Requis si `Timed` (15 min ou 1 h à partir du clic) |

Persistée dans les réglages à chaque changement pour survivre au redémarrage (FR-026).

---

## 5. LogEvent — entrée de journal

| Champ | Type | Exemple |
|---|---|---|
| `Timestamp` | horodatage local ISO 8601 | `2026-10-27T14:03:12+01:00` |
| `Kind` | énumération | `AppStart`, `AppStop`, `StateChanged`, `AlertPresented`, `AlertPresentFailed`, `Snooze`, `PauseStarted`, `PauseEnded`, `SettingsSaved`, `SettingsReset`, `SoundFallback`, `HotkeyUnavailable`, `Info` (ex. second lancement, seuils Debug remplacés), `Error` |
| `Detail` | texte | `Normal → AlertHigh à 80 % (secteur présent)` |

Format d'une ligne : `<Timestamp> [<Kind>] <Detail>`.
