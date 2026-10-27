<!--
Sync Impact Report
==================
Version change: (template vierge) → 1.0.0
Bump rationale: première ratification — remplacement complet du modèle par des principes concrets.

Modified principles (placeholder → titre) :
- [PRINCIPLE_1_NAME] → I. Rappel actif pour la santé de la batterie
- [PRINCIPLE_2_NAME] → II. Discrétion et non-intrusion
- [PRINCIPLE_3_NAME] → III. L'utilisateur garde le contrôle
- [PRINCIPLE_4_NAME] → IV. Moteur testable, indépendant du matériel
- [PRINCIPLE_5_NAME] → V. Robustesse et observabilité
- (ajouté) → VI. Simplicité et périmètre MVP

Added sections :
- Contraintes techniques (ex-[SECTION_2_NAME])
- Workflow de développement et qualité (ex-[SECTION_3_NAME])
- Principe VI (le modèle n'en prévoyait que 5)

Removed sections : aucune

Templates / dépendances :
- .specify/templates/plan-template.md      ✅ compatible (lit la constitution pour la "Constitution Check", non modifié)
- .specify/templates/spec-template.md      ✅ compatible (non modifié)
- .specify/templates/tasks-template.md     ✅ compatible (non modifié)
- CLAUDE.md / README.md                    ✅ cohérents avec la section Workflow (non modifiés)

Deferred TODOs : aucun placeholder laissé.
Décisions produit volontairement NON tranchées ici (relèvent de /speckit-clarify) :
idee.md §10 (repartir de zéro vs faire évoluer BatterieEtat) et §11 (nom définitif,
notification au démarrage, répétition plafonnée, son en boucle, durées de pause,
mode plein écran, multi-batteries, mode de distribution).
-->

# BatNotif Constitution

## Core Principles

### I. Rappel actif pour la santé de la batterie

BatNotif existe pour **protéger la santé de la batterie**, pas pour afficher son niveau.

- L'alerte DOIT être pilotée par la combinaison **secteur × niveau**, jamais par un simple
  franchissement de seuil : alerte basse = sur batterie (ou batterie qui se décharge
  malgré le chargeur) **et** `niveau ≤ seuil bas` ; alerte haute = sur secteur **et**
  `niveau ≥ seuil haut`.
- Tant que la condition d'alerte est vraie et que l'utilisateur n'a pas mis en pause ou
  en snooze, le rappel DOIT se répéter à l'intervalle configuré.
- Une **hystérésis** DOIT empêcher les alertes en boucle autour d'un seuil
  (fin d'alerte à `seuil bas + 2` / `seuil haut − 2` par défaut).
- Les changements d'état du secteur DOIVENT être filtrés (debounce d'environ 2 s) pour
  ignorer les micro-coupures.
- Après une sortie de veille ou d'hibernation, l'état DOIT être réévalué immédiatement.

**Pourquoi** : la notification Windows à 20 % est fugace et ne dit rien à 100 % branché.
La valeur du produit tient entièrement à un rappel fiable et insistant, sans faux positif.

### II. Discrétion et non-intrusion

- L'application DOIT tourner en arrière-plan, avec une icône dans la zone de notification,
  sans impact perceptible sur les performances (CPU au repos négligeable, polling de
  secours espacé d'environ 5 s minimum).
- La fenêtre de notification DOIT rester au premier plan **sans jamais voler le focus**.
- L'application NE DOIT JAMAIS demander d'élévation de privilèges (pas d'admin, pas de
  service Windows) : tout passe par l'espace utilisateur (`HKCU`, `%AppData%`).
- Une **seule instance** DOIT pouvoir tourner (mutex nommé).
- Fermer la fenêtre principale NE DOIT PAS quitter l'application : seul « Quitter » le fait.

**Pourquoi** : un outil qui tourne en permanence n'est accepté que s'il ne gêne jamais.

### III. L'utilisateur garde le contrôle

- Les seuils, l'intervalle de rappel, le volume et le son de chaque type d'alerte DOIVENT
  être réglables depuis l'interface, avec des contraintes validées
  (`seuil bas < seuil haut`, hystérésis cohérente).
- Un bouton **Pause / Reprendre** DOIT être accessible dans la fenêtre et dans le menu
  de l'icône tray. L'état de pause DOIT être visible et DOIT survivre à un redémarrage.
- Les réglages DOIVENT être persistés dans `%AppData%\BatNotif\settings.json` et
  rechargés au démarrage ; un fichier absent ou corrompu DOIT entraîner un repli sur les
  valeurs par défaut, sans plantage.
- L'état courant (niveau, secteur, état d'alerte ou de pause, prochain rappel) DOIT être
  lisible en un coup d'œil.

**Pourquoi** : un rappel insistant n'est supportable que si l'utilisateur peut le régler
et le suspendre facilement.

### IV. Moteur testable, indépendant du matériel

- La logique d'alerte (machine à états, hystérésis, répétition, pause, snooze) DOIT vivre
  dans un composant **sans dépendance à WPF ni aux API Windows**, alimenté par une
  abstraction de la source batterie et de l'horloge.
- Ce moteur DOIT être couvert par des **tests unitaires automatisés** sur batterie et
  horloge simulées, couvrant chaque état et chaque transition de la machine à états,
  ainsi que les cas limites (oscillation autour du seuil, micro-coupure secteur, sortie
  de veille, chargeur trop faible, pause persistée).
- Pour le moteur, les tests DOIVENT être écrits avant ou en même temps que le code, et un
  changement de règle d'alerte NE DOIT PAS être fusionné sans test correspondant.
- La lecture et l'écriture des réglages DOIVENT aussi être testées (y compris le fichier
  corrompu).

**Pourquoi** : on ne peut pas attendre qu'une vraie batterie passe à 80 % pour vérifier
une règle ; la fiabilité du principe I repose sur ces tests.

### V. Robustesse et observabilité

- Chaque cas limite listé dans `idee.md` §9 DOIT avoir un comportement défini : absence
  de batterie (PC fixe, signalé clairement à l'utilisateur), fichier audio déplacé ou
  supprimé (repli sur un son par défaut + avertissement), veille, multi-écrans (afficher
  sur l'écran de la fenêtre active), batterie à 100 % branchée.
- Aucune exception non gérée NE DOIT faire disparaître l'application silencieusement.
- L'application DOIT journaliser ses actions importantes (lectures batterie significatives,
  transitions d'état, alertes émises, pause et reprise, erreurs) dans un fichier de log
  local lisible sous `%AppData%\BatNotif\`.
- Les scripts et commandes du projet DOIVENT produire des logs exhaustifs avec des étapes
  numérotées (`[n/N] …`), sans jamais masquer la sortie (voir `CLAUDE.md` §5).

**Pourquoi** : une application qui tourne en permanence doit pouvoir être diagnostiquée
après coup, sans débogueur.

### VI. Simplicité et périmètre MVP

- Le périmètre de référence est le **MVP** de `idee.md` §4.1, complété par les éléments
  de confort de §4.2 au fil des features.
- Les fonctionnalités « plus tard » de `idee.md` §4.3 (statistiques, courbes de santé,
  voix, coupure matérielle de la charge, thèmes) NE DOIVENT PAS être implémentées sans
  mise à jour préalable de `idee.md` et une nouvelle spec.
- Toute nouvelle dépendance NuGet DOIT être justifiée dans le `plan.md` de la feature ;
  on préfère les API de la plateforme quand elles suffisent.
- Pas d'abstraction spéculative : une couche n'est ajoutée que si elle sert un test
  (principe IV) ou un besoin réel.

**Pourquoi** : un petit utilitaire fiable vaut mieux qu'un gros utilitaire inachevé.

## Contraintes techniques

- **Plateforme** : Windows 10 (19041) et plus, PC portable avec batterie.
- **Langage / UI** : C# + WPF, **.NET 8**, TFM `net8.0-windows10.0.19041.0`.
- **Batterie** : `Windows.Devices.Power.Battery` + événement `ReportUpdated`, avec un
  polling de secours ; secteur via `SystemInformation.PowerStatus` (ou équivalent).
- **Tray** : `H.NotifyIcon.Wpf`. **Audio** : `NAudio` (ou `MediaPlayer` WPF si suffisant).
- **Réglages** : JSON via `System.Text.Json`, dans `%AppData%\BatNotif\`.
- **Démarrage auto** : clé `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Notification** : fenêtre WPF maison sans bordure, pas de toast Windows.
- **Tests** : projet de tests .NET séparé (framework choisi dans le premier `plan.md`).
- Les choix ouverts de `idee.md` §10 (partir de zéro ou faire évoluer `BatterieEtat`) et
  §11 (dont le mode de distribution) sont tranchés dans les specs via `/speckit-clarify`,
  puis reportés dans `idee.md`.

## Workflow de développement et qualité

- Chaque feature suit le cycle Spec Kit : `/speckit-specify` → (`/speckit-clarify`) →
  `/speckit-plan` → `/speckit-tasks` → (`/speckit-analyze`) → `/speckit-implement`.
  On NE passe PAS à l'étape suivante sans validation explicite de l'utilisateur.
- `idee.md` est la source de l'idée : toute décision qui change le périmètre ou tranche une
  question ouverte DOIT y être reportée. Une spec qui contredit `idee.md` DOIT être alignée.
- `README.md` DOIT refléter l'état réel du projet ; il est mis à jour après chaque étape
  terminée et vérifié avant chaque commit.
- Chaque `plan.md` DOIT contenir une vérification de conformité à cette constitution et
  justifier toute exception.
- Avant de déclarer une tâche terminée : le build passe (`dotnet build`), les tests passent
  (`dotnet test`), et le comportement concerné a été vérifié.
- Commits : contenu et message exhaustif montrés à l'utilisateur, confirmation explicite,
  puis commit et push ensemble. Aucun commit silencieux.

## Governance

- Cette constitution prime sur toute autre pratique du projet. En cas de conflit avec une
  spec, un plan ou une tâche, c'est la constitution qui s'applique, ou bien on l'amende.
- **Amendement** : proposé via `/speckit-constitution`, avec un Sync Impact Report, validé
  par l'utilisateur, puis commité. `idee.md`, `CLAUDE.md` et `README.md` sont alignés si
  nécessaire.
- **Versionnage sémantique** : MAJOR = suppression ou redéfinition incompatible d'un
  principe ; MINOR = nouveau principe ou section, ou extension notable ; PATCH =
  clarification ou formulation.
- **Conformité** : la « Constitution Check » de chaque `plan.md` et `/speckit-analyze`
  vérifient le respect des principes ; toute complexité ajoutée DOIT être justifiée.
- `CLAUDE.md` contient les consignes de travail au quotidien (logs, workflow, fichiers de
  référence).

**Version**: 1.0.0 | **Ratified**: 2026-10-27 | **Last Amended**: 2026-10-27
