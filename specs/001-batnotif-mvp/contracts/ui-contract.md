# Contrat — interfaces utilisateur

Ce que l'utilisateur voit et peut faire. Sert de référence aux tests manuels du [quickstart](../quickstart.md).

## 1. Icône de zone de notification (FR-028, FR-029)

| Élément | Comportement |
|---|---|
| Apparence | Distincte pour : **normal**, **alerte** (basse/haute), **pause**, **aucune batterie** |
| Info-bulle | `BatNotif — 74 % · branché · normal` (niveau, secteur, état) |
| Clic gauche | Ouvre la fenêtre principale si masquée, la masque si visible |
| Clic droit | Menu ci-dessous |

Menu contextuel :

| Entrée | Visible quand | Action |
|---|---|---|
| Ouvrir | toujours | Affiche la fenêtre principale au premier plan |
| Pause 15 min / Pause 1 h / Pause indéfinie | pas en pause | Démarre la pause correspondante |
| Reprendre | en pause | Termine la pause, réévalue immédiatement |
| Quitter | toujours | Ferme complètement l'application |

## 2. Raccourci global (FR-031)

`Ctrl+Alt+B` → affiche la fenêtre principale au premier plan, depuis n'importe quelle application.
S'il est déjà pris par une autre application : avertissement dans la fenêtre principale + journal.

## 3. Fenêtre principale (FR-017 à FR-020, FR-023, FR-033)

Zones, de haut en bas :

1. **État** : niveau (%) + barre, secteur (branché / sur batterie), état courant
   (`Normal`, `Alerte basse`, `Alerte haute`, `Pause jusqu'à HH:MM`, `Pause indéfinie`, `Snooze jusqu'à HH:MM`,
   `Aucune batterie détectée`).
2. **Alerte basse** : champ « Brancher à [__] % » + bouton ↺ (défaut 20), son (nom du fichier ou « Son intégré »)
   + « Choisir… » + « Intégré » (revenir au son par défaut) + ▶ Tester.
3. **Alerte haute** : champ « Débrancher à [__] % » + bouton ↺ (défaut 80), son + « Choisir… » + « Intégré » + ▶ Tester.
4. **Volume** : curseur 0–100 %.
5. **Démarrer avec Windows** : case à cocher.
6. **Actions** : bouton Pause (choix 15 min / 1 h / indéfiniment) ou Reprendre, bouton Enregistrer.
7. **Avertissements** (si besoin) : fichier son introuvable, réglages réinitialisés, raccourci indisponible,
   erreur empêchant les alertes.

Règles :
- « Enregistrer » refuse une saisie invalide et affiche la contrainte (ex. « Le seuil haut doit dépasser le seuil
  bas d'au moins 5 % »).
- Fermer la fenêtre (✕) la masque ; l'application continue (FR-030).
- « Choisir… » ouvre un sélecteur filtré sur les fichiers audio (`*.mp3`, `*.wav`).

## 4. Notification d'alerte (FR-011 à FR-016, FR-037)

| Aspect | Mode normal | Mode renforcé (plein écran) |
|---|---|---|
| Position | Coin haut-droit de l'écran de la fenêtre active | Centrée sur cet écran |
| Taille | Compacte (≈ 440 × 120 px à 100 %, place pour le bouton Snooze) | Agrandie (≈ 2× plus grande, texte plus gros) |
| Focus | Ne prend **jamais** le focus | Passe au premier plan et **peut** prendre le focus |
| Contenu | Icône du type d'alerte, message, niveau mis à jour en direct | Idem, plus visible |
| Message bas | « Batterie à 18 % — branche le chargeur » | idem |
| Message haut | « Batterie à 82 % — débranche le chargeur » | idem |
| Bouton | **Snooze 15 min** (seul bouton ; pas de « fermer ») | idem |
| Son | Son du type d'alerte en boucle, au volume réglé | idem |
| Disparition | Fin de l'alerte, snooze ou pause, uniquement | idem |

Si la fenêtre plein écran passe en mode fenêtré (ou l'inverse) pendant une alerte, la notification bascule de
mode au prochain contrôle (≤ 5 s).
