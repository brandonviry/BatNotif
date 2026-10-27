# Research — 001-batnotif-mvp

**Date** : 2026-10-27
**Entrées** : [spec.md](spec.md), constitution v1.1.0. Aucune autre source projet n'est utilisée
(`idee.md` §10 : projet indépendant, développé de zéro).

Chaque décision suit le format : **Décision / Pourquoi / Alternatives écartées**.

---

## R1. Technologie d'interface (réévaluation neutre)

Besoins issus de la spec qui départagent les technos :

| Besoin (spec) | Exigence |
|---|---|
| FR-011 | Fenêtre maison sans bordure, toujours au premier plan, **sans prendre le focus** |
| FR-037 | En plein écran : passer devant le jeu, format agrandi, **pouvoir prendre le focus** |
| FR-028/029 | Icône de zone de notification avec menu, icône qui change selon l'état |
| FR-015 | Son en boucle continue (MP3/WAV), volume réglable |
| FR-034 | Aucun droit administrateur |
| SC-006 | Consommation négligeable au repos |
| §11.8 | `.exe` simple à distribuer |
| Constitution VI | Peu de dépendances, pas d'abstraction spéculative |

Technos comparées : uniquement **natives et légères** (Tauri / Electron exclus d'office : moteur web
embarqué, trop lourd pour un utilitaire permanent).

| Critère | **WPF** | **WinUI 3** | **WinForms** |
|---|---|---|---|
| Fenêtre custom sans bordure, transparente, arrondie | Natif et simple (XAML, `AllowsTransparency`) | Possible mais via `AppWindow` + interop | Possible mais dessin manuel (GDI), rendu moins net |
| Topmost sans vol de focus (`WS_EX_NOACTIVATE`) | `Topmost` + `ShowActivated=false` + 1 style Win32 | Pas de `Topmost` natif, tout en interop Win32 | `TopMost` + `ShowWithoutActivation` natifs |
| Icône tray | Pas native → 1 bibliothèque | Pas native → bibliothèque + interop | **Native** (`NotifyIcon`) |
| Haute résolution / multi-écrans DPI | Bon (vectoriel) | Très bon | Moyen (mise à l'échelle bitmap) |
| `.exe` single-file autonome | Supporté | Mal supporté (Windows App Runtime) | Supporté |
| Mémoire au repos (ordre de grandeur) | ~40–70 Mo | ~60–100 Mo | ~20–40 Mo |
| Maturité / documentation | Très mature | Récent, API encore mouvante | Très mature |

**Décision** : **WPF**.

**Pourquoi** : la fenêtre de notification maison est le cœur visible du produit (FR-011, FR-037). WPF est
le seul des trois qui permet de la faire belle et nette à toute résolution **et** qui gère simplement
topmost / non-activation. WinUI 3 demanderait de l'interop pour presque chaque besoin et complique la
distribution. WinForms est plus léger en mémoire et a un tray natif, mais rendrait la notification
agrandie / soignée nettement plus coûteuse à réaliser et moins nette en haute résolution.
L'écart de mémoire (quelques dizaines de Mo) reste compatible avec SC-006, qui porte sur le CPU et
l'autonomie.

**Alternatives écartées** : WinUI 3 (interop omniprésente, packaging), WinForms (rendu custom limité),
Tauri / Electron (trop lourds).

## R2. Version de .NET

**Décision** : **.NET 10 (LTS)**, TFM `net10.0-windows10.0.19041.0` pour l'application, `net10.0` pour le
moteur.

**Pourquoi** : .NET 8 (prévu dans `idee.md` et la constitution) arrive en **fin de support le 2026-11-10**,
soit deux semaines après aujourd'hui. .NET 10 est la LTS actuelle (support jusqu'en novembre 2028), son SDK
(10.0.114) est installé, et il apporte `TimeProvider` en standard, utile pour tester le moteur (R9).

**Alternatives écartées** : .NET 8 (fin de support imminente), .NET 9 (STS, support plus court que 10).

> Validé par l'utilisateur et intégré à la constitution **v1.2.0** (section « Contraintes techniques »).

## R3. Lecture de l'état batterie / secteur

**Décision** : `Windows.System.Power.PowerManager` (WinRT) :
`RemainingChargePercent`, `BatteryStatus` (`NotPresent` / `Discharging` / `Idle` / `Charging`),
`PowerSupplyStatus` (`NotPresent` / `Inadequate` / `Adequate`), avec ses événements
`RemainingChargePercentChanged`, `BatteryStatusChanged`, `PowerSupplyStatusChanged`. Plus un **polling de
secours toutes les 5 s** qui relit les mêmes propriétés.

**Pourquoi** : une seule API couvre les trois entrées du moteur. `PowerSupplyStatus = Inadequate` et
`BatteryStatus = Discharging` sur secteur donnent directement le cas « chargeur trop faible » (edge case de
la spec). `BatteryStatus = NotPresent` couvre FR-005. Le polling de secours garantit FR-002 (≤ 10 s) même si
un événement est manqué.

**Alternatives écartées** : `Windows.Devices.Power.Battery` (rapport en mWh, à convertir, ne donne pas
directement l'état du secteur) ; `SystemInformation.PowerStatus` (impose WinForms, pas de notion de chargeur
insuffisant) ; WMI `Win32_Battery` (lent, lourd).

## R4. Sortie de veille

**Décision** : `Microsoft.Win32.SystemEvents.PowerModeChanged` (`PowerModes.Resume`) → réévaluation immédiate
(FR-004), plus une détection de « saut d'horloge » dans le polling (écart > 30 s entre deux ticks) comme filet
de sécurité.

**Pourquoi** : API standard, sans droits. Le filet de sécurité couvre les cas où l'événement arrive en retard.

**Alternatives écartées** : `RegisterSuspendResumeNotification` en P/Invoke direct (plus de code pour le même
résultat).

## R5. Notification sans vol de focus, et alerte renforcée en plein écran

**Décision** :
- **Mode normal** : fenêtre WPF `WindowStyle=None`, `Topmost=true`, `ShowActivated=false`, avec les styles
  étendus `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` (pas de focus, absente d'Alt+Tab). Position : coin haut-droit
  de la zone de travail de l'écran qui contient la fenêtre au premier plan (`GetForegroundWindow` →
  `MonitorFromWindow`).
- **Détection plein écran** : `SHQueryUserNotificationState` (`QUNS_RUNNING_D3D_FULL_SCREEN`, `QUNS_BUSY`,
  `QUNS_PRESENTATION_MODE`) **ou** fenêtre au premier plan dont les bornes couvrent tout l'écran.
- **Mode renforcé** (FR-037) : même fenêtre en variante agrandie et centrée, **sans** `WS_EX_NOACTIVATE`, puis
  mise au premier plan forcée (`SetForegroundWindow`, avec le contournement documenté de la restriction de
  premier plan : simulation d'une pression de touche Alt avant l'appel). Un jeu en plein écran exclusif perd
  alors le focus et se réduit, ce qui est le comportement voulu (« couper en plein jeu »).

**Pourquoi** : couvre FR-011 et FR-037 avec la même fenêtre et deux variantes, sans dépendance.

**Risque identifié** : Windows peut refuser `SetForegroundWindow` dans certains cas. Mitigation : la fenêtre
reste `Topmost`, le son en boucle continue (FR-015), et l'échec est journalisé (FR-036). À valider manuellement
avec un jeu réel (voir [quickstart.md](quickstart.md)).

**Alternatives écartées** : toasts Windows (exclus par la spec, invisibles en plein écran exclusif).

## R6. Audio en boucle

**Décision** : **NAudio** — `AudioFileReader` (MP3 / WAV) dans un flux bouclé, sortie `WaveOut` (nom de
`WaveOutEvent` depuis NAudio 3), volume via `VolumeSampleProvider`. NAudio 3 lit dans un `Span<float>`
(`ISampleProvider.Read(Span<float>)`). **Sons par défaut générés par le code** (deux motifs de bips distincts, basse /
haute, via les générateurs de signal de NAudio) : aucun fichier audio externe à embarquer.

**Pourquoi** : boucle sans coupure audible, volume fin, formats MP3 et WAV (hypothèse de la spec), tout en
restant une dépendance unique et très répandue. Générer les sons par défaut évite tout asset et sert aussi de
repli si le fichier choisi est introuvable (FR-016).

**Alternatives écartées** : `MediaPlayer` WPF (boucle via `MediaEnded` avec micro-coupure, nécessite le thread
UI) ; `SoundPlayer` (WAV uniquement).

## R7. Icône de zone de notification

**Décision** : **H.NotifyIcon.Wpf**, avec des icônes **générées par le code** pour les états normal / alerte /
pause / aucune batterie (pastille de couleur + pourcentage), sans fichier `.ico` externe pour les états.

**Pourquoi** : bibliothèque WPF dédiée (menu contextuel WPF, clic gauche / droit), évite d'activer WinForms
juste pour le tray, et permet de générer l'icône dynamiquement (FR-028).

**Alternatives écartées** : `System.Windows.Forms.NotifyIcon` (oblige à mélanger WinForms et WPF).

## R8. Raccourci global, instance unique, démarrage automatique

**Décision** :
- **Raccourci** `Ctrl+Alt+B` : `RegisterHotKey` (Win32) sur une fenêtre de messages dédiée. Si le raccourci
  est déjà pris par une autre application : journaliser et afficher un avertissement discret dans la fenêtre.
- **Instance unique** : `Mutex` nommé par utilisateur (`Local\BatNotif.SingleInstance`) + `EventWaitHandle`
  nommé pour demander à l'instance existante d'afficher sa fenêtre (FR-032).
- **Démarrage auto** : valeur dans `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` pointant vers l'exe
  courant (FR-022), sans droits admin.

**Pourquoi** : API système standard, zéro dépendance, aucun droit admin.

## R9. Testabilité du moteur

**Décision** : le moteur est un projet **`BatNotif.Core`** en `net10.0` **sans référence Windows / WPF** :
il reçoit des instantanés batterie via une interface et le temps via **`TimeProvider`**. Tests avec **xUnit**
et **`Microsoft.Extensions.TimeProvider.Testing`** (`FakeTimeProvider`) pour simuler debounce, snooze, pause et
sauts de veille sans attendre.

**Pourquoi** : la séparation est imposée par le compilateur (un projet `net10.0` ne peut pas appeler WPF), ce
qui garantit le principe IV. `TimeProvider` est l'abstraction officielle du temps depuis .NET 8.

**Alternatives écartées** : NUnit / MSTest (équivalents, xUnit est le plus répandu dans l'écosystème .NET
actuel) ; horloge maison (`TimeProvider` existe déjà).

## R10. Réglages et journal

**Décision** :
- **Réglages** : `System.Text.Json`, fichier `%AppData%\BatNotif\settings.json`, écriture atomique (fichier
  temporaire puis remplacement) pour ne jamais laisser un fichier à moitié écrit ; fichier illisible → renommé
  en `settings.corrupt-<horodatage>.json`, valeurs par défaut chargées, utilisateur prévenu (FR-021).
- **Journal** : petit journal fichier maison dans `%AppData%\BatNotif\logs\batnotif-<date>.log`, une ligne
  horodatée par événement, rotation quotidienne, conservation 14 jours (FR-036).

**Pourquoi** : aucune dépendance supplémentaire (constitution VI), besoins simples.

**Alternatives écartées** : Serilog / NLog (dépendance pour un besoin minimal).

## R11. Distribution (`idee.md` §11.8)

**Décision** : **`.exe` unique autonome** (`win-x64`, self-contained, single-file), sans installeur, publié en
**deux versions** par `scripts/publish.ps1` (étapes numérotées) :

| Version | Compression | Disque | Mémoire privée au repos | Pour qui |
|---|---|---|---|---|
| **standard** (`publish/standard/`) | non | 159,6 Mo | ≈ 133 Mo | Recommandée : l'app tourne en permanence, la RAM compte plus que le disque |
| **compact** (`publish/compact/`) | oui | 68,8 Mo | ≈ 194 Mo | Le plus petit fichier possible |

**Pourquoi** : l'utilisateur copie un seul fichier et le lance, sans installer le runtime .NET ni avoir de
droits admin. Le démarrage auto passe par `HKCU` (R8), un installeur n'apporte donc rien au MVP.

**Mesures (T061, 2026-10-27)** : CPU moyen au repos **0,004 %** sur 10 min (SC-006 respecté). La compression de
l'exe unique décompresse toutes les DLL en mémoire au lancement : **+60 Mo de RAM** environ. Sans compression,
la consommation est identique à un build classique (≈ 133 Mo, socle runtime + WPF + bibliothèques natives) ;
les réglages du ramasse-miettes (GC non concurrent, `GCConserveMemory`) n'y changent rien (133,1 Mo).
L'objectif interne « < 100 Mo » du plan initial était irréaliste pour WPF ; il est remplacé par ces valeurs
de référence. Choix des deux versions fait par l'utilisateur.

L'alternative « framework-dependent » ferait ~2 Mo mais exigerait d'installer le .NET Desktop Runtime 10.

**Alternatives écartées** : installeur MSI / Inno Setup (complexité inutile pour le MVP) ; framework-dependent
(dépendance à installer pour l'utilisateur final).

## R12. Seuils, hystérésis et validation

**Décision** : seuils réglables de 5 % à 95 %, hystérésis fixe de 2 %. Contrainte de validation :
`seuil bas + 2 < seuil haut − 2`, soit **`seuil haut − seuil bas ≥ 5`**, pour que les zones de fin d'alerte ne
se chevauchent pas (FR-018).

**Pourquoi** : traduit directement les hypothèses de la spec en une règle testable.

---

## Récapitulatif des dépendances NuGet (justification exigée par la constitution VI)

| Paquet | Version épinglée | Projet | Justification |
|---|---|---|---|
| `H.NotifyIcon.Wpf` | 2.4.1 | App | Icône tray WPF (R7), pas d'équivalent WPF natif |
| `NAudio` | 3.3.0 | App | Boucle audio MP3/WAV sans coupure + sons générés (R6) |
| `xunit` | 2.9.3 | Tests | Framework de test (R9) |
| `xunit.runner.visualstudio` | 4.0.1 | Tests | Exécution des tests par `dotnet test` |
| `Microsoft.NET.Test.Sdk` | 18.11.0 | Tests | Hôte de test |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.11.0 | Tests | `FakeTimeProvider` pour simuler le temps (R9) |

SDK fixé par `global.json` : .NET 10.0.100, `rollForward: latestFeature` (installé : 10.0.114).

Tout le reste (WinRT `PowerManager`, `System.Text.Json`, `Mutex`, registre, `RegisterHotKey`) fait partie de la
plateforme.
