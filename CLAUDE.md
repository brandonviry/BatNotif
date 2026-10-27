# CLAUDE.md — BatNotif

Instructions pour Claude Code dans ce projet. Ce fichier explique comment le projet est organisé avec **Spec Kit** et les règles de travail à respecter.

---

## 0. Périmètre de travail : uniquement ce dossier

- On travaille **exclusivement dans `D:\bat_notif`**. Aucun autre dossier du disque n'est lu, listé, exploré ni
  réutilisé, même si un document cite son chemin.
- BatNotif est **indépendant** : aucun code, asset ni choix technique n'est repris d'un autre projet.
- Si un document mentionne un chemin ou un projet externe : le signaler à l'utilisateur et lui demander, sans
  jamais l'ouvrir.

## 1. L'idée du projet : `idee.md`

- `idee.md` (à la racine du projet) est **le document de référence de l'idée** : pitch, problème, fonctionnalités, machine à états, interface, stack, cas limites, décisions à trancher, roadmap.
- Il **reste à la racine** du projet et doit être **rédigé proprement et tenu à jour** : dès qu'une décision est prise (par ex. une réponse à une question du §11), dès qu'une fonctionnalité change ou dès que le périmètre bouge, on met `idee.md` à jour.
- C'est la **source d'entrée** de `/speckit-specify` : les specs de Spec Kit en découlent, pas l'inverse. Si une spec contredit `idee.md`, on aligne les deux et on signale l'écart à l'utilisateur.

## 1 bis. `README.md` : obligatoire, à la racine, toujours à jour

- Le projet a un `README.md` **à la racine**, **bien rédigé** (en français) et **tenu à jour**.
- Il présente le projet à quelqu'un qui le découvre : ce que fait l'app, son état d'avancement, la stack, comment compiler / lancer / publier, l'organisation du dépôt (renvoi vers `idee.md`, `specs/`, `CLAUDE.md`).
- **On le met à jour à chaque changement significatif** : nouvelle feature implémentée, étape du workflow Spec Kit terminée, changement de stack, nouvelle commande de build ou de lancement, changement de l'état d'avancement. On vérifie systématiquement que le README est à jour avant de proposer un commit.
- Différence avec `idee.md` : `idee.md` = l'idée et le cahier des charges (le **quoi / pourquoi** détaillé) ; `README.md` = la vitrine et le mode d'emploi du projet **tel qu'il existe réellement**.

---

## 2. Spec Kit : c'est quoi ?

**Spec Kit** (GitHub, CLI `specify`, version installée : **0.16.2**) est une boîte à outils de **Spec-Driven Development** (développement piloté par les spécifications). Le principe : on ne code pas directement à partir d'une idée floue. On passe par des documents successifs, chacun relu et validé :

> principes du projet → spécification (le **quoi** / le **pourquoi**) → plan technique (le **comment**) → liste de tâches → implémentation

Chaque étape est une **commande (skill) Claude Code** `/speckit-*`, installée dans `.claude/skills/`.

Installation faite avec :

```powershell
specify init --here --integration claude --script ps
```

- `--integration claude` : les commandes sont générées comme skills Claude Code.
- `--script ps` : les scripts d'aide sont en PowerShell (Windows).
- Numérotation des features : **séquentielle** (`001-...`, `002-...`).
- L'extension `git` de Spec Kit n'est **pas** installée : les commandes ne créent pas de branches git automatiquement, seulement les dossiers de spec.

---

## 3. Arborescence Spec Kit

```
bat_notif/
├── CLAUDE.md                  ← ce fichier
├── README.md                  ← présentation + mode d'emploi du projet (à tenir à jour)
├── idee.md                    ← l'idée / cahier des charges (à tenir à jour)
├── .claude/
│   └── skills/
│       └── speckit-*/SKILL.md ← les commandes /speckit-* (ne pas modifier à la main)
├── .specify/                  ← infrastructure Spec Kit
│   ├── memory/
│   │   └── constitution.md    ← la constitution : principes non négociables du projet
│   ├── templates/             ← modèles utilisés par les commandes
│   │   ├── constitution-template.md
│   │   ├── spec-template.md
│   │   ├── plan-template.md
│   │   ├── tasks-template.md
│   │   └── checklist-template.md
│   ├── scripts/powershell/    ← scripts appelés par les commandes
│   │   ├── create-new-feature.ps1   (crée specs/NNN-nom/ + spec.md)
│   │   ├── setup-plan.ps1           (prépare plan.md)
│   │   ├── setup-tasks.ps1          (prépare tasks.md)
│   │   ├── check-prerequisites.ps1  (vérifie que les docs requis existent)
│   │   ├── resolve-template.ps1
│   │   └── common.ps1
│   ├── workflows/speckit/workflow.yml ← enchaînement complet specify → plan → tasks → implement avec validations
│   ├── integration.json, init-options.json, integrations/ ← config de l'installation (ne pas modifier)
│   └── .gitignore             ← ignore feature.json (pointeur local vers la feature courante)
└── specs/                     ← créé par /speckit-specify, un dossier par feature
    └── 001-nom-feature/
        ├── spec.md            ← spécification fonctionnelle
        ├── plan.md            ← plan technique
        ├── research.md, data-model.md, contracts/, quickstart.md ← artefacts du plan
        ├── checklists/        ← checklists qualité (/speckit-checklist)
        └── tasks.md           ← tâches ordonnées
```

Règles :
- On **ne modifie pas à la main** `.claude/skills/speckit-*`, `.specify/scripts/` ni les fichiers de config `.specify/*.json` : ils sont gérés par le CLI `specify`.
- `constitution.md` et les fichiers dans `specs/` sont **à nous** : ils sont produits par les commandes puis relus et validés par l'utilisateur.

---

## 4. Workflow de développement

Ordre à suivre pour chaque feature. **On ne passe pas à l'étape suivante sans validation de l'utilisateur.**

| # | Commande | Rôle | Produit |
|---|---|---|---|
| 1 | `/speckit-constitution` | Fixer les principes du projet (stack, qualité, tests, contraintes). **Une fois** au début, puis seulement en cas de changement de cap. | `.specify/memory/constitution.md` |
| 2 | `/speckit-specify` | Décrire **quoi** construire et **pourquoi** (sans technique), à partir de `idee.md`. | `specs/NNN-.../spec.md` |
| 2b | `/speckit-clarify` *(optionnel, recommandé)* | Poser jusqu'à 5 questions ciblées pour lever les ambiguïtés de la spec (ex. décisions du §11 de `idee.md`). Avant le plan. | `spec.md` complété |
| 3 | `/speckit-plan` | Décider **comment** : architecture, stack, modèle de données, contrats. | `plan.md`, `research.md`, `data-model.md`… |
| 3b | `/speckit-checklist` *(optionnel)* | Vérifier que les exigences sont complètes, claires et cohérentes. | `checklists/*.md` |
| 4 | `/speckit-tasks` | Découper le plan en tâches ordonnées par dépendances. | `tasks.md` |
| 4b | `/speckit-analyze` *(optionnel, recommandé)* | Contrôler la cohérence spec ↔ plan ↔ tâches, sans rien modifier. Avant l'implémentation. | rapport |
| 5 | `/speckit-implement` | Exécuter les tâches de `tasks.md` une par une. | code |
| 6 | `/speckit-converge` *(si besoin)* | Comparer le code à la spec / au plan et ajouter le travail manquant dans `tasks.md`. | `tasks.md` complété |

Résumé : **constitution → specify → (clarify) → plan → tasks → (analyze) → implement**.

`/speckit-taskstoissues` permet de convertir `tasks.md` en issues GitHub (seulement quand un remote GitHub existera).

Après une étape qui change une décision ou le périmètre : **mettre à jour `idee.md`**. Après chaque étape terminée (et surtout après `/speckit-implement`) : **mettre à jour `README.md`**.

---

## 5. Logs des commandes : obligatoires et exhaustifs

L'utilisateur veut **voir ce qui se passe**. Chaque commande exécutée (build, restore, install, tests, scripts, publish…) doit produire des **logs exhaustifs et lisibles**.

- **Annoncer chaque étape avec sa numérotation** avant de la lancer, par exemple :
  ```
  [1/3] Installation de NAudio…
  [2/3] Restauration des paquets (dotnet restore)…
  [3/3] Compilation (dotnet build)…
  ```
- **Ne jamais masquer la sortie** : pas de `| Out-Null`, pas de `> $null` / `2>$null` / `> NUL`, pas de flags silencieux (`-q`, `--quiet`, `--silent`, `-nologo` qui cache des infos utiles), et pas de filtrage de la sortie (`| Select-String`, `| Select-Object -Last N`, `| head`, `| tail`).
- Privilégier la verbosité quand l'outil la propose (ex. `dotnet build -v normal` si `minimal` ne montre pas assez).
- **Commandes en arrière-plan** : ne pas rediriger vers un fichier log par défaut, laisser la sortie défiler. Si une redirection est vraiment nécessaire, le dire et donner le chemin du fichier.
- **À la fin de chaque commande**, recopier dans la conversation la partie utile de la sortie (étapes clés, warnings, erreurs, dernières lignes) et indiquer clairement succès ou échec.
- Les **scripts et le code de l'application** suivent la même règle : chaque script écrit dans la console des messages d'étape numérotés (`[n/N] …`), et l'app journalise ses actions importantes (lecture batterie, transitions d'état, alertes, erreurs).
- Logs textuels uniquement, pas de captures d'écran du terminal.

---

## 6. Barre de progression à la fin de chaque message

Dès que l'implémentation a commencé, **chaque message** à l'utilisateur se termine par un bloc de progression
calculé à partir de `specs/<feature>/tasks.md` (tâches cochées `[X]` / total) :

```
Progression de l'implémentation : ██████░░░░░░░░░░░░░░ 26 % (17/65 tâches)

✅ Fait
- Phase 1 · Setup (6/6)
- Phase 2 · Fondations (11/11)

⏳ Reste à faire
- US1 · Alertes continues — jalon interne (0/17)
- US2 · Réglages (0/11)
- US3 · Snooze et pause → première version livrable (0/8)
- US4 · Tray complet, raccourci (0/5)
- Finition · Exe autonome, mesures, docs (0/7)
```

Règles :
- Barre de **20 caractères** (`█` = fait, `░` = reste), une case = 5 %, pourcentage arrondi à l'entier.
- Les chiffres viennent **toujours** de `tasks.md` (recompter, ne pas estimer) ; une phase commencée apparaît dans
  « Reste à faire » avec son compteur (ex. `US1 (9/17)`) jusqu'à ce qu'elle soit complète.
- Le bloc est placé **tout à la fin** du message, après tout le reste.
