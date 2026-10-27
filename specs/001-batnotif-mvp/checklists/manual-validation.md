# Validation manuelle — 001-batnotif-mvp

Résultats des scénarios du [quickstart](../quickstart.md) et des mesures. Une ligne par scénario :
**OK** / **KO** + observations. Rempli au fil des stories (tâches de vérification T034, T045, T053, T059, T061–T063).

## US1 — Alertes continues (T034)

Contrôles automatiques (2026-10-27) :

- [x] Build solution sans warning ni erreur
- [x] 42 tests unitaires verts (debounce, transitions, continuité, types de base)
- [x] Démarrage : le processus reste actif (pas de plantage au lancement)
- [x] Instance unique : un second lancement se ferme seul (code 0)

Scénarios manuels (à faire par l'utilisateur) :

| # | Scénario | Commande / étapes | Résultat | Observations |
|---|---|---|---|---|
| M1 | Alerte haute, sans perte de focus | Chargeur branché : `dotnet run --project src/BatNotif.App -- --high <niveau−1>`, puis taper dans un éditeur | ☐ | |
| M2 | Fin d'alerte ≤ 10 s | Pendant M1 : débrancher | ☐ | |
| M3 | Alerte continue | Laisser M1 tourner 3 min | ☐ | |
| M7 | Démarrage en zone d'alerte | Relancer branché avec `--high <niveau−1>` → alerte immédiate ; débranché → rien | ☐ | |
| M8 | Plein écran | Lancer un jeu / vidéo plein écran puis déclencher M1 | ☐ | |
| M15 | Sortie de veille | Déclencher M1, mettre en veille, réveiller | ☐ | |

Quitter l'application : clic droit sur l'icône verte « B » → **Quitter**.

## US2 — Réglages (T045)

*À venir.*

## US3 — Snooze et pause (T053)

*À venir.*

## US4 — Tray complet (T059)

*À venir.*

## Finition (T061–T063)

*À venir.*
