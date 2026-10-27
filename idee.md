# BatNotif — Notificateur de santé de batterie

> Document d'idée / cahier des charges initial. À mettre à jour au fil des décisions.

---

## 1. Pitch

Une application Windows discrète qui tourne en arrière-plan et **prévient l'utilisateur, avec SA musique**, dès que la batterie sort de sa zone de confort :

- **trop basse** → il faut brancher le chargeur ;
- **trop haute** → il faut débrancher le chargeur.

L'objectif n'est pas d'informer sur le niveau de batterie (Windows le fait déjà), c'est de **protéger la santé de la batterie** en évitant les deux extrêmes : la décharge profonde et la charge complète prolongée.

C'est un rappel actif, pas une notification passive.

---

## 2. Le problème

Les batteries Li-ion vieillissent plus vite quand elles restent :

- **sous ~20 %** pendant longtemps (décharge profonde) ;
- **au-dessus de ~80 %** en étant branchées (tension haute prolongée, chaleur).

Windows affiche bien une notification à 20 %, mais :

- elle est **fugace**, facile à rater ;
- elle ne dit **rien** quand la batterie est pleine et toujours branchée ;
- elle n'est **pas personnalisable** (pas de son choisi, pas de répétition, pas de pause).

Résultat : on laisse le PC branché à 100 % toute la nuit, ou on le laisse mourir à 5 %.

---

## 3. Principe

Un **état** de batterie (`niveau` + `branché/débranché`) pilote une **machine à états** simple. Quand une condition d'alerte est vraie, l'app affiche une **notification custom** (fenêtre WPF maison, pas un toast Windows) accompagnée de la **musique choisie par l'utilisateur pour ce type d'alerte**, et **répète** le rappel tant que la condition n'est pas levée.

L'utilisateur garde le contrôle :

- seuils réglables dans une GUI ;
- son personnalisable par type d'alerte ;
- bouton **Pause** (dans la fenêtre et dans le menu de l'icône système) ;
- l'app reste ouverte en tray même quand la fenêtre est fermée.

---

## 4. Fonctionnalités

### 4.1 Cœur (MVP)

| # | Fonctionnalité | Détail |
|---|---|---|
| 1 | Surveillance en arrière-plan | Icône dans la zone de notification, aucun impact perceptible |
| 2 | Détection du niveau | Lecture périodique + événement système si disponible |
| 3 | Détection branché / débranché | `AC online` / `on battery`, avec debounce |
| 4 | Seuil bas | Défaut **20 %**, réglable |
| 5 | Seuil haut | Défaut **80 %**, réglable |
| 6 | Notification visuelle custom | Fenêtre maison, coin haut-droit, toujours au premier plan, sans vol de focus |
| 7 | Son personnalisable | Un fichier audio par type d'alerte (bas / haut) |
| 8 | Répétition | Tant que la condition est vraie, rappel toutes les N minutes |
| 9 | Pause / Reprendre | Bouton dans l'app + entrée du menu tray |
| 10 | Réglages persistants | Sauvegardés et rechargés au démarrage |

### 4.2 Confort

- **Hystérésis** pour éviter les alertes en boucle quand le niveau oscille autour du seuil (ex. 79 ↔ 80 %).
- **Intervalle de rappel réglable** (1 à 30 min).
- **Volume** de la notification réglable, avec bouton **« Tester »**.
- **Démarrage automatique avec Windows** (option).
- **Snooze** dans la notification : « Rappelle-moi dans 15 min ».
- **Instance unique** (mutex) : pas de doublons en tray.
- **Icône tray** avec menu : Ouvrir, Pause, Reprendre, Quitter.
- **Raccourci global** (ex. `Ctrl+Alt+B`) pour rouvrir la fenêtre, car Windows 11 cache les nouvelles icônes dans le tiroir `^`.

### 4.3 Plus tard (hors MVP)

- Statistiques / historique du niveau de batterie.
- Courbe de santé / cycles de charge.
- Notification vocale (« Batterie à 80 %, tu peux débrancher »).
- Lancement d'action automatique (couper la charge si le matériel le permet — rarement exposé sous Windows).
- Thèmes / skins de la fenêtre de notification.

---

## 5. Règles de notification (machine à états)

### 5.1 Entrées

| Variable | Source |
|---|---|
| `level` | Pourcentage de batterie (0–100) |
| `acOnline` | Présence du secteur |
| `discharging` | Batterie qui descend réellement (cas du chargeur trop faible) |

### 5.2 Paramètres

| Paramètre | Défaut | Rôle |
|---|---|---|
| `lowThreshold` | 20 % | Déclenche l'alerte « branche » |
| `highThreshold` | 80 % | Déclenche l'alerte « débranche » |
| `lowReset` | `lowThreshold + 2` | Fin d'alerte quand on remonte au-dessus |
| `highReset` | `highThreshold - 2` | Fin d'alerte quand on redescend en dessous |
| `repeatInterval` | 5 min | Délai entre deux rappels |
| `pauseUntil` | — | Pause temporaire ou indéfinie |

### 5.3 États

| État | Condition | Comportement |
|---|---|---|
| `NORMAL` | aucune condition d'alerte | silencieux |
| `ALERTE_BASSE` | pas de secteur **et** `level ≤ lowThreshold` | notification + rappel répété |
| `ALERTE_HAUTE` | secteur **et** `level ≥ highThreshold` | notification + rappel répété |
| `PAUSE` | l'utilisateur a mis en pause | silencieux, l'état réel est quand même suivi |

### 5.4 Transitions

| Depuis | Événement | Vers |
|---|---|---|
| `NORMAL` | secteur perdu **et** `level ≤ low` | `ALERTE_BASSE` |
| `NORMAL` | secteur présent **et** `level ≥ high` | `ALERTE_HAUTE` |
| `ALERTE_BASSE` | `level ≥ lowReset` **ou** secteur branché | `NORMAL` |
| `ALERTE_HAUTE` | `level ≤ highReset` **ou** secteur perdu | `NORMAL` |
| `NORMAL` / `ALERTE_*` | clic sur Pause | `PAUSE` |
| `PAUSE` | fin de pause ou clic sur Reprendre | réévalue l'état réel |

### 5.5 Points de conception importants

- **L'alerte est pilotée par `branché × niveau`, pas par un simple franchissement de seuil.** Être à 82 % débranché est normal ; être à 82 % branché ne l'est pas.
- **Debounce sur le secteur** : Windows peut signaler des micro-coupures. Ignorer les changements de moins de ~2 s.
- **Pas de notification au démarrage si l'état est déjà en zone d'alerte ?** → *à trancher* (voir §11).
- **Après une reprise de veille**, on réévalue immédiatement : une alerte peut avoir été manquée.
- **Répétition infinie par défaut**, conforme à la demande. Une option « s'arrêter après N rappels » peut exister mais reste désactivée.
- **La pause doit survivre à un redémarrage** de l'app (stockée avec un timestamp ou un flag « indéfinie »).

---

## 6. L'interface

### 6.1 Fenêtre principale

```
┌─────────────────────────────────────────────┐
│  BatNotif                              — □ × │
├─────────────────────────────────────────────┤
│  Batterie   ● 74 %      ⚡ Branchée          │
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░                       │
│                                             │
│  Alerte batterie basse                      │
│   Brancher à  [ 20 ] %   ↺                 │
│   Son      [ low.mp3        ] [Choisir] ▶  │
│                                             │
│  Alerte batterie haute                      │
│   Débrancher à [ 80 ] %   ↺                 │
│   Son      [ full.mp3       ] [Choisir] ▶  │
│                                             │
│  Rappel toutes les [ 5 ] min                │
│  Volume  [───────●────]  [Tester]           │
│                                             │
│  ☐ Démarrer avec Windows                    │
│                                             │
│  [  ⏸ Pause  ]        [ Enregistrer ]       │
└─────────────────────────────────────────────┘
```

- L'état courant doit être **lisible en un coup d'œil** : niveau, secteur, état de veille, prochain rappel.
- Les seuils sont contraints : `lowThreshold < highThreshold`, avec un reset qui respecte l'hystérésis.
- Bouton **Pause** bien visible, avec un choix de durée (15 min / 1 h / indéfiniment).

### 6.2 Icône tray

- Clic gauche : ouvre / masque la fenêtre.
- Clic droit : `Ouvrir` · `Pause` / `Reprendre` · `Quitter`.
- L'icône change légèrement selon l'état (normale / alerte / pause) pour être repérable.

### 6.3 La notification

- Petite fenêtre WPF **sans bordure**, coin **haut-droit de l'écran actif**.
- **Toujours au premier plan**, **ne vole pas le focus**.
- Contenu : icône + message clair (« Batterie à 82 % — débranche le chargeur ») + boutons `Vu` et `Snooze 15 min`.
- Se ferme au clic ou après un délai.
- **Le son est joué en même temps**, une fois par rappel (ou en boucle jusqu'à fermeture — option).
- Ne pas apparaître **en plein écran / en jeu** si l'option « mode plein écran » est cochée.

---

## 7. Réglages à persister

```json
{
  "lowThreshold": 20,
  "highThreshold": 80,
  "repeatIntervalMinutes": 5,
  "lowSoundPath": "%USERPROFILE%\\Music\\low.mp3",
  "highSoundPath": "%USERPROFILE%\\Music\\full.mp3",
  "volume": 0.8,
  "startWithWindows": true,
  "pausedUntil": null,
  "pauseIndefinite": false,
  "silentInFullscreen": true
}
```

Stockage : `%AppData%\BatNotif\settings.json`. Repli sur des valeurs par défaut si le fichier est absent ou corrompu.

---

## 8. Stack technique proposée

| Brique | Choix | Pourquoi |
|---|---|---|
| Langage / UI | **C# + WPF, .NET 8** | Mature sous Windows, fenêtres custom faciles, expérience déjà acquise |
| TFM | `net8.0-windows10.0.19041.0` | Accès aux API batterie WinRT |
| Batterie | `Windows.Devices.Power.Battery` + `BatteryStatusChanged` | Événementiel, précis ; polling de secours toutes les ~5 s |
| Secteur | `System.Windows.Forms.SystemInformation.PowerStatus` | Simple et fiable |
| Tray | `H.NotifyIcon.Wpf` | Éprouvé, WPF-friendly |
| Audio | `NAudio` ou `MediaPlayer` WPF | MP3/WAV, volume, boucle |
| Réglages | JSON + `System.Text.Json` | Zéro dépendance, lisible |
| Auto-start | Clé `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | Pas besoin d'admin |
| Instance unique | `Mutex` nommé | Évite les doublons |

Aucune élévation de privilèges requise.

---

## 9. Cas limites à gérer

- **Chargeur branché mais batterie qui baisse** (chargeur faible, USB) → traiter comme « sur batterie » pour l'alerte basse.
- **Veille / hibernation** → réévaluer au réveil, éventuellement notifier immédiatement si en zone d'alerte.
- **Oscillation autour du seuil** → hystérésis obligatoire.
- **Batterie à 100 % branchée** → l'alerte haute persiste, c'est le but.
- **Aucune batterie** (PC fixe) → l'app doit le détecter et le dire clairement, pas tourner dans le vide.
- **Fichier audio déplacé ou supprimé** → repli sur un son par défaut + avertissement dans l'UI.
- **Ne pas déranger / Focus Assist** → la fenêtre custom passe outre, mais le son reste soumis au mixeur système. À documenter.
- **Multi-écrans** → afficher sur l'écran de la fenêtre active.
- **Pause** → ne doit pas être perdue au redémarrage, et doit être visible dans le tray.

---

## 10. Base existante réutilisable

Un projet `BatterieEtat` existe déjà sur la machine :

- chemin : `D:\notification_bat\BatterieEtat`
- stack : C# WPF .NET 8, `H.NotifyIcon.Wpf`, `NAudio`, toasts Windows
- contient déjà : lecture batterie, seuils LOW/FULL paramétrables, hystérésis, icône tray, mutex, assets audio et icône.

**Différences avec la présente idée :**

| Point | BatterieEtat | BatNotif |
|---|---|---|
| Notification | toast Windows | fenêtre custom + musique |
| Son | fichiers fixes | **choisi par l'utilisateur, par type** |
| Répétition | à confirmer | **rappel répété explicite** |
| Pause | à confirmer | **exigée** |

→ Décision à prendre : **repartir de zéro** dans `D:\bat_notif` (plus propre, l'idée est plus riche) ou **forker / faire évoluer** `BatterieEtat`. Le squelette batterie + tray est de toute façon réutilisable.

---

## 11. Décisions à trancher

1. **Nom définitif** : BatNotif, BatteryGuard, ChargeGuard, autre ?
2. **Notification au démarrage** si déjà en zone d'alerte : silencieuse ou immédiate ?
3. **Répétition** : infinie par défaut, ou plafonnée avec un mode « insistant » activable ?
4. **Son** : joué une fois par rappel, ou en boucle jusqu'à interaction ?
5. **Pause** : bouton unique ou choix de durée (15 min / 1 h / indéfiniment) ?
6. **Mode plein écran / jeu** : masquer la notification ou l'afficher quand même ?
7. **Multi-utilisateurs / multi-batteries** : hors périmètre ?
8. **Distribution** : `.exe` single-file auto-suffisant, ou installeur (MSI / Inno Setup) ?

---

## 12. Roadmap suggérée

| Étape | Contenu | Critère de fin |
|---|---|---|
| **1. Squelette** | Projet WPF, tray, mutex, lecture batterie + secteur, fenêtre minimale | L'état batterie s'affiche en temps réel |
| **2. Moteur** | Machine à états, hystérésis, répétition, pause | Alertes correctes sur batterie simulée |
| **3. Notification** | Fenêtre custom, son, snooze, position multi-écran | Alerte visible et audible, sans vol de focus |
| **4. Réglages** | GUI complète, persistance JSON, autostart, test audio | Tout survit à un redémarrage |
| **5. Finition** | Mode plein écran, icône d'état, packaging single-file, tests veille | `exe` autonome prêt à l'emploi |

---

*Brouillon initial — à compléter avec les réponses aux décisions du §11.*
