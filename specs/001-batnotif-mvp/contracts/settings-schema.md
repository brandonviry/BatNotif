# Contrat — fichier de réglages

**Emplacement** : `%AppData%\BatNotif\settings.json` (encodage UTF-8).
**Modèle logique** : [data-model.md §2](../data-model.md).

## Format (version 1)

```json
{
  "schemaVersion": 1,
  "lowThreshold": 20,
  "highThreshold": 80,
  "lowSoundPath": "",
  "highSoundPath": "",
  "volume": 0.8,
  "startWithWindows": false,
  "pause": {
    "kind": "None",
    "until": null
  }
}
```

| Clé | Type JSON | Obligatoire | Notes |
|---|---|---|---|
| `schemaVersion` | number | oui | `1` pour le MVP ; permet de migrer plus tard |
| `lowThreshold` | number (entier) | oui | 5–95 |
| `highThreshold` | number (entier) | oui | 5–95, `highThreshold − lowThreshold ≥ 5` |
| `lowSoundPath` | string | non | `""` ou absent = son intégré |
| `highSoundPath` | string | non | idem |
| `volume` | number | oui | 0.0–1.0 |
| `startWithWindows` | boolean | oui | — |
| `pause.kind` | string | oui | `"None"`, `"Timed"`, `"Indefinite"` |
| `pause.until` | string (ISO 8601 UTC) ou `null` | si `Timed` | ex. `"2026-10-27T15:00:00Z"` |

## Règles de lecture

1. Fichier absent → valeurs par défaut, fichier créé au premier enregistrement.
2. JSON illisible → fichier renommé `settings.corrupt-<yyyyMMdd-HHmmss>.json`, valeurs par défaut, avertissement
   dans la fenêtre, entrée de journal `SettingsReset`.
3. Clé inconnue → ignorée. Clé manquante → valeur par défaut pour cette clé.
4. Valeur hors bornes ou combinaison invalide → valeur par défaut pour les champs concernés + avertissement.
5. `pause.kind = "Timed"` avec `until` dépassé → traité comme `None`.

## Règles d'écriture

- Écriture atomique : `settings.json.tmp` puis remplacement de `settings.json`.
- Écrit à chaque « Enregistrer » et à chaque changement de pause.
- Clés en camelCase, JSON indenté (lisible à la main).
