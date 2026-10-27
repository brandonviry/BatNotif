# Validation manuelle — 001-batnotif-mvp

Résultats des scénarios du [quickstart](../quickstart.md) et des mesures. Une ligne par scénario :
**OK** / **KO** + observations. Rempli au fil des stories (tâches de vérification T034, T045, T053, T059, T061–T063).

## US1 — Alertes continues (T034)

Contrôles automatiques (2026-10-27) :

- [x] Build solution sans warning ni erreur
- [x] 42 tests unitaires verts (debounce, transitions, continuité, types de base)
- [x] Démarrage : le processus reste actif (pas de plantage au lancement)
- [x] Instance unique : un second lancement se ferme seul (code 0)

Scénarios manuels — validés par l'utilisateur le 2026-10-27 :

| # | Scénario | Commande / étapes | Résultat | Observations |
|---|---|---|---|---|
| M1 | Alerte haute, sans perte de focus | Chargeur branché : `dotnet run --project src/BatNotif.App -- --high <niveau−1>`, puis taper dans un éditeur | ✅ OK | |
| M2 | Fin d'alerte ≤ 10 s | Pendant M1 : débrancher | ✅ OK | |
| M3 | Alerte continue | Laisser M1 tourner 3 min | ✅ OK | |
| M7 | Démarrage en zone d'alerte | Relancer branché avec `--high <niveau−1>` → alerte immédiate ; débranché → rien | ✅ OK | |
| M8 | Plein écran | Lancer un jeu / vidéo plein écran puis déclencher M1 | ✅ OK | |
| M15 | Sortie de veille | Déclencher M1, mettre en veille, réveiller | ✅ OK | |

- [x] US1 validée manuellement (T034)


Quitter l'application : clic droit sur l'icône verte « B » → **Quitter**.

## US2 — Réglages (T045)

Contrôles automatiques (2026-10-27) :

- [x] Build solution sans warning ni erreur
- [x] 75 tests unitaires verts (+33 : validation, persistance settings.json, seuils à chaud)
- [x] Démarrage avec lecture des réglages : le processus reste actif
- [x] Second lancement : se ferme et affiche la fenêtre « BatNotif » de la première instance

Scénarios manuels — validés par l'utilisateur le 2026-10-27 :

| # | Scénario | Étapes | Résultat | Observations |
|---|---|---|---|---|
| M9 | Réglages persistés | Clic droit « B » → Ouvrir ; changer seuils, sons, volume ; Enregistrer ; Quitter ; relancer | ✅ OK | |
| M10 | Validation | Seuil bas 78, seuil haut 80, Enregistrer | ✅ OK | |
| M11 | Son introuvable | Choisir un MP3, Enregistrer, renommer le fichier, provoquer une alerte | ✅ OK | |
| M12 | Réglages corrompus | Quitter, écrire du texte invalide dans `%AppData%\BatNotif\settings.json`, relancer | ✅ OK | |
| M16 | Démarrage Windows | Cocher, Enregistrer, fermer la session, rouvrir | ✅ OK | |
| SC-005 | Prise en main < 2 min | Depuis une installation neuve : régler les 2 seuils, choisir et tester les 2 sons | ✅ OK | Moins de 2 min (durée exacte non relevée) |

- [x] US2 validée manuellement (T045)

## US3 — Snooze et pause (T053)

Contrôles automatiques (2026-10-27) :

- [x] Build solution sans warning ni erreur
- [x] 94 tests unitaires verts (+19 : snooze, pause minutée / indéfinie, reprise, persistance au démarrage)
- [x] Démarrage : le processus reste actif ; second lancement → fenêtre « BatNotif » affichée

Scénarios manuels — validés par l'utilisateur le 2026-10-27 :

| # | Scénario | Étapes | Résultat | Observations |
|---|---|---|---|---|
| M4 | Snooze | Déclencher une alerte (`--high <niveau−1>`), cliquer « Snooze 15 min » | ✅ OK | |
| M5 | Pause persistée | Clic droit « B » → Pause 1 h ; Quitter ; relancer | ✅ OK | |
| M6 | Reprendre | En pause avec la condition d'alerte vraie → Reprendre (fenêtre ou tray) | ✅ OK | |

- [x] US3 validée manuellement (T053) → **première version livrable** (US1 + US2 + US3)

## US4 — Tray complet (T058)

Contrôles automatiques (2026-10-27) :

- [x] Build solution sans warning ni erreur ; 94 tests unitaires verts
- [x] Démarrage : le processus reste actif
- [x] `Ctrl+Alt+B` détenu par BatNotif pendant l'exécution (un second enregistrement échoue) et libéré à l'arrêt

Scénarios manuels — validés par l'utilisateur le 2026-10-27 :

| # | Scénario | Étapes | Résultat | Observations |
|---|---|---|---|---|
| M13a | Icône d'état | Observer la pastille : verte + niveau (normal), rouge / ambre (alerte), grise (pause) ; survoler → info-bulle « BatNotif — 74 % · branché · normal » | ✅ OK | |
| M13b | Clic gauche | Clic gauche sur la pastille → fenêtre affichée ; nouveau clic gauche → masquée | ✅ OK | |
| M13c | Fermer ≠ quitter | Fermer la fenêtre (✕) → l'icône reste, les alertes continuent | ✅ OK | |
| M13d | Raccourci | Fenêtre masquée, depuis une autre application : `Ctrl+Alt+B` → fenêtre au premier plan | ✅ OK | |
| M13e | Quitter | Clic droit → Quitter → l'icône disparaît, plus aucun processus BatNotif | ✅ OK | |
| M14 | Instance unique | Lancer l'app deux fois → une seule icône, la fenêtre existante s'affiche | ✅ OK | |

- [x] US4 validée manuellement (T058)

## Finition (T060–T062 : exe publié, mesures, journal)

*À venir.*
