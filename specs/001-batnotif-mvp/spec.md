# Feature Specification: BatNotif MVP — notificateur de santé de batterie

**Feature Branch**: `001-batnotif-mvp`

**Created**: 2026-10-27

**Status**: Draft

**Input**: User description: "MVP de BatNotif tel que décrit dans idee.md (§1-§7, §9 ; périmètre MVP §4.1 + confort §4.2 ; exclure §4.3). Source de référence : idee.md. Respecter la constitution v1.0.0."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Être alerté et relancé quand la batterie sort de sa zone de confort (Priority: P1)

L'utilisateur travaille sur son PC portable. BatNotif tourne en arrière-plan sans qu'il y pense. Quand la batterie
descend trop bas alors qu'il est sur batterie, une notification visible apparaît avec un son et lui dit de brancher
le chargeur. Quand la batterie monte trop haut alors que le chargeur est branché, une notification lui dit de le
débrancher. Tant qu'il ne corrige pas la situation, la notification reste affichée et le son tourne en boucle,
sans interruption. Dès qu'il branche (ou débranche) le chargeur, ou que le niveau revient dans la zone de confort,
la notification disparaît et le son s'arrête.

**Why this priority**: c'est la raison d'être du produit (protéger la batterie des deux extrêmes). Avec les seuils
et le son par défaut, cette histoire seule apporte déjà toute la valeur principale.

**Independent Test**: avec les réglages par défaut (20 % / 80 %), laisser le PC branché jusqu'à 80 % (ou simuler
l'état batterie) et vérifier qu'une notification sonore apparaît, se répète, puis s'arrête au débranchement.
Même chose sur batterie à 20 %.

**Acceptance Scenarios**:

1. **Given** le chargeur est branché et la batterie est à 79 %, **When** la batterie atteint 80 %, **Then** une
   notification « débranche le chargeur » s'affiche avec le niveau actuel et le son de l'alerte haute est joué.
2. **Given** le PC est sur batterie à 21 %, **When** la batterie descend à 20 %, **Then** une notification
   « branche le chargeur » s'affiche avec le niveau actuel et le son de l'alerte basse est joué.
3. **Given** une alerte haute est active, **When** l'utilisateur ne fait rien pendant plusieurs minutes,
   **Then** la notification reste affichée et le son continue de tourner en boucle sans interruption.
4. **Given** une alerte haute est active, **When** l'utilisateur débranche le chargeur, **Then** l'alerte prend
   fin : la notification disparaît et le son s'arrête en 10 s au plus.
5. **Given** une alerte basse est active, **When** l'utilisateur branche le chargeur et que la batterie
   recommence à charger, **Then** l'alerte prend fin.
6. **Given** le PC est sur batterie à 82 %, **When** rien ne change, **Then** aucune alerte n'est émise (être haut
   en étant débranché est normal).
7. **Given** une alerte haute vient de se terminer parce que le niveau est redescendu à 78 %, **When** le niveau
   oscille entre 78 % et 79 %, **Then** aucune nouvelle alerte n'est émise tant que 80 % n'est pas de nouveau
   atteint.
8. **Given** une notification est affichée, **When** l'utilisateur est en train de taper dans une autre
   application fenêtrée, **Then** sa saisie n'est pas interrompue (la notification ne prend pas le focus).
9. **Given** l'utilisateur joue à un jeu en plein écran avec le chargeur branché, **When** la batterie atteint
   80 %, **Then** l'alerte interrompt la session : elle passe devant le jeu, en format agrandi et plus voyant
   qu'une notification normale, avec le son.
10. **Given** le PC est branché à 90 %, **When** l'utilisateur lance BatNotif, **Then** l'alerte haute est émise
    immédiatement.
11. **Given** le PC est sur batterie à 90 %, **When** l'utilisateur lance BatNotif, **Then** aucune alerte n'est
    émise.

---

### User Story 2 - Régler les seuils, les sons et le volume (Priority: P2)

L'utilisateur ouvre la fenêtre principale. Il voit en un coup d'œil le niveau de batterie, l'état du chargeur,
l'état des alertes et, le cas échéant, l'heure de fin de la pause ou du snooze. Il règle le seuil bas et le seuil
haut, choisit un fichier audio pour chaque type d'alerte, règle le volume, et écoute le résultat avec un bouton
« Tester ». Il enregistre. Après redémarrage du PC, tout est conservé.

**Why this priority**: la personnalisation (et surtout « sa » musique) est la deuxième promesse du produit, mais
l'application est déjà utile avec les valeurs par défaut.

**Independent Test**: modifier chaque réglage, enregistrer, quitter puis relancer l'application et vérifier que
toutes les valeurs sont restaurées et appliquées aux alertes suivantes.

**Acceptance Scenarios**:

1. **Given** la fenêtre principale est ouverte, **When** l'utilisateur la regarde, **Then** il voit le niveau de
   batterie, l'état branché/débranché, l'état courant (normal, alerte basse, alerte haute, pause, snooze) et
   l'heure de fin de la pause ou du snooze s'il y en a une.
2. **Given** l'utilisateur saisit un seuil bas supérieur ou égal au seuil haut, **When** il tente d'enregistrer,
   **Then** l'enregistrement est refusé avec un message clair indiquant la contrainte.
3. **Given** l'utilisateur a choisi un fichier audio pour l'alerte haute, **When** il clique sur « Tester »,
   **Then** ce son est joué au volume réglé.
4. **Given** l'utilisateur a enregistré des réglages, **When** l'application redémarre, **Then** tous les réglages
   sont restaurés à l'identique.
5. **Given** l'utilisateur a baissé le volume à 40 %, **When** une alerte se déclenche, **Then** le son tourne en
   boucle à 40 %.
6. **Given** l'utilisateur coche « Démarrer avec Windows », **When** il redémarre sa session, **Then** BatNotif est
   lancé automatiquement en arrière-plan.

---

### User Story 3 - Faire taire l'alerte temporairement (Priority: P2)

L'utilisateur ne peut pas corriger la situation tout de suite (réunion, pas de chargeur à portée). L'alerte ne
s'arrête jamais d'elle-même : depuis la notification, il peut cliquer sur « Snooze 15 min » pour la faire taire et
être alerté de nouveau plus tard.
Depuis la fenêtre ou le menu de l'icône, il peut mettre BatNotif en pause pour 15 min, 1 h ou indéfiniment, puis
reprendre quand il veut. La pause reste visible et survit à un redémarrage de l'application.

**Why this priority**: un rappel insistant n'est acceptable que s'il peut être suspendu facilement ; sans cette
histoire, l'utilisateur finirait par fermer l'application.

**Independent Test**: déclencher une alerte, cliquer sur Snooze et vérifier que l'alerte se tait et ne revient
pas avant 15 min ;
mettre en pause 1 h, redémarrer l'application et vérifier que la pause est toujours active et visible.

**Acceptance Scenarios**:

1. **Given** une notification d'alerte est affichée, **When** l'utilisateur ne clique sur rien, **Then** la
   notification reste affichée et le son continue (aucune fermeture automatique, aucun bouton pour simplement
   fermer).
2. **Given** une notification d'alerte est affichée, **When** l'utilisateur clique sur « Snooze 15 min », **Then**
   la notification disparaît, le son s'arrête, et l'alerte revient (notification + son en boucle) au bout de
   15 min si la condition est toujours vraie.
3. **Given** l'application est en marche, **When** l'utilisateur choisit « Pause 1 h » dans le menu de l'icône,
   **Then** aucune alerte n'est émise pendant 1 h, l'icône et la fenêtre indiquent la pause et son heure de fin.
4. **Given** une pause est active, **When** l'utilisateur clique sur « Reprendre », **Then** l'état réel de la
   batterie est réévalué immédiatement et une alerte est émise si la condition est vraie.
5. **Given** une pause indéfinie est active, **When** l'application est relancée, **Then** la pause est toujours
   active et visible.
6. **Given** une pause de 15 min arrive à son terme, **When** la batterie est en zone d'alerte, **Then** une alerte
   est émise immédiatement.

---

### User Story 4 - Présence discrète et toujours accessible (Priority: P3)

BatNotif vit dans la zone de notification de Windows. Son icône change selon l'état (normal, alerte, pause). Un
clic gauche ouvre ou masque la fenêtre ; un clic droit propose Ouvrir, Pause/Reprendre et Quitter. Fermer la
fenêtre ne quitte pas l'application. Comme Windows 11 cache souvent les nouvelles icônes, un raccourci clavier
global (`Ctrl+Alt+B`) rouvre la fenêtre. Lancer l'application une deuxième fois ne crée pas de doublon.

**Why this priority**: améliore le confort et la découvrabilité, mais les alertes fonctionnent sans.

**Independent Test**: fermer la fenêtre et vérifier que les alertes continuent ; utiliser le raccourci global
pour la rouvrir ; lancer l'application deux fois et vérifier qu'une seule icône existe.

**Acceptance Scenarios**:

1. **Given** la fenêtre principale est ouverte, **When** l'utilisateur la ferme, **Then** l'application continue
   de tourner dans la zone de notification et les alertes restent actives.
2. **Given** la fenêtre est masquée, **When** l'utilisateur appuie sur `Ctrl+Alt+B`, **Then** la fenêtre s'affiche
   au premier plan.
3. **Given** BatNotif tourne déjà, **When** l'utilisateur le lance à nouveau, **Then** aucune deuxième instance
   n'est créée et la fenêtre de l'instance existante s'affiche.
4. **Given** une alerte est active, **When** l'utilisateur regarde la zone de notification, **Then** l'icône est
   visuellement différente de l'état normal et de l'état pause.
5. **Given** l'utilisateur choisit « Quitter » dans le menu de l'icône, **When** il confirme, **Then**
   l'application se ferme complètement.

---

### Edge Cases

- **Chargeur branché mais batterie qui baisse** (chargeur trop faible, USB) : traité comme « sur batterie » pour
  l'alerte basse ; l'alerte basse se déclenche si le niveau atteint le seuil bas.
- **Micro-coupures du secteur** : un changement branché/débranché qui dure moins de 2 s est ignoré.
- **Oscillation autour d'un seuil** : l'alerte ne se termine qu'au franchissement du seuil de fin (seuil bas + 2 %,
  seuil haut − 2 %) et ne redémarre qu'au franchissement du seuil de déclenchement.
- **Batterie à 100 % branchée** : l'alerte haute persiste et le son continue en boucle, c'est le but.
- **Utilisateur absent du PC** (alerte active pendant des heures) : la notification et le son en boucle continuent
  jusqu'à correction, snooze ou pause.
- **Sortie de veille ou d'hibernation** : l'état est réévalué immédiatement au réveil ; si la batterie est en zone
  d'alerte, l'alerte est émise immédiatement.
- **Aucune batterie détectée** (PC fixe) : l'application l'indique clairement dans la fenêtre et l'icône, et
  n'émet aucune alerte.
- **Fichier audio déplacé ou supprimé** : un son par défaut intégré est joué à la place, et un avertissement est
  affiché dans la fenêtre à côté du réglage concerné.
- **Réglages absents ou corrompus** : l'application démarre avec les valeurs par défaut et le signale, sans
  planter.
- **Multi-écrans** : la notification apparaît dans le coin haut-droit de l'écran où se trouve la fenêtre active.
- **Ne pas déranger / Focus Assist** : la notification visuelle s'affiche quand même ; le son reste soumis au
  volume et au mixeur du système (comportement documenté pour l'utilisateur).
- **Application en plein écran (jeu, vidéo)** : l'utilisateur est entièrement concentré et ne voit rien d'autre ;
  l'alerte est donc **renforcée** : elle interrompt la session en passant devant l'application plein écran, dans un
  format agrandi et plus voyant, avec le son. C'est la seule situation où la notification peut prendre le focus.
  Si l'application plein écran empêche tout affichage par-dessus, l'alerte la fait sortir du plein écran (la
  réduit) pour s'afficher.
- **Démarrage de l'application alors que la batterie est déjà en zone d'alerte** : l'état est évalué
  immédiatement selon la règle normale (secteur × niveau) ; si la condition est vraie, l'alerte est émise tout de
  suite (branché à 90 % → alerte ; sur batterie à 90 % → rien).

## Requirements *(mandatory)*

### Functional Requirements

**Surveillance**

- **FR-001**: Le système DOIT surveiller en continu, en arrière-plan, le niveau de batterie (0–100 %), la présence
  du secteur et le fait que la batterie se décharge réellement.
- **FR-002**: Le système DOIT détecter un changement de niveau ou d'état du secteur en 10 s au maximum.
- **FR-003**: Le système DOIT ignorer les changements d'état du secteur durant moins de 2 s.
- **FR-004**: Le système DOIT évaluer l'état de la batterie immédiatement au démarrage de l'application et après
  une sortie de veille ou d'hibernation, et émettre l'alerte sans attendre si la condition est vraie.
- **FR-005**: Le système DOIT détecter l'absence de batterie, l'indiquer clairement et ne pas émettre d'alerte
  dans ce cas.

**Règles d'alerte**

- **FR-006**: Le système DOIT entrer en **alerte basse** quand le PC est sur batterie (ou que la batterie se
  décharge malgré le chargeur) **et** que le niveau est inférieur ou égal au seuil bas (défaut 20 %).
- **FR-007**: Le système DOIT entrer en **alerte haute** quand le secteur est présent **et** que le niveau est
  supérieur ou égal au seuil haut (défaut 80 %).
- **FR-008**: Le système DOIT terminer l'alerte basse quand le niveau atteint le seuil bas + 2 % ou quand le
  secteur est branché et que la batterie charge ; et terminer l'alerte haute quand le niveau descend au seuil
  haut − 2 % ou quand le secteur est débranché.
- **FR-009**: Le système NE DOIT PAS émettre d'alerte quand le niveau est haut sur batterie, ni quand le niveau est
  bas sur secteur avec batterie en charge.
- **FR-010**: Tant qu'une alerte est active, hors pause et hors snooze, la notification DOIT rester affichée et le
  son DOIT tourner en boucle sans interruption ni limite de durée ; ils ne s'arrêtent qu'à la fin de l'alerte
  (FR-008), sur snooze ou sur pause.

**Notification**

- **FR-011**: Chaque alerte DOIT afficher une notification visuelle propre à l'application
  (pas la notification standard de Windows), dans le coin haut-droit de l'écran actif, toujours au premier plan,
  sans prendre le focus clavier — sauf en plein écran (voir FR-037).
- **FR-012**: La notification DOIT afficher le niveau de batterie et une consigne claire
  (ex. « Batterie à 82 % — débranche le chargeur »), avec une icône correspondant au type d'alerte.
- **FR-013**: La notification DOIT proposer l'action « Snooze 15 min » ; elle NE DOIT PAS proposer de bouton pour
  simplement la fermer.
- **FR-014**: La notification NE DOIT PAS se fermer automatiquement ; elle disparaît uniquement à la fin de
  l'alerte, sur snooze ou sur pause. Le niveau affiché DOIT se mettre à jour tant qu'elle est visible.
- **FR-015**: Le son associé au type d'alerte DOIT démarrer en même temps que la notification et tourner en boucle,
  au volume configuré, jusqu'à ce que la notification disparaisse.
- **FR-016**: Si le fichier audio choisi est introuvable ou illisible, le système DOIT jouer un son par défaut
  intégré et signaler le problème dans la fenêtre principale.

**Réglages**

- **FR-017**: L'utilisateur DOIT pouvoir régler : seuil bas, seuil haut, fichier audio de l'alerte basse, fichier
  audio de l'alerte haute, volume (défaut 80 %), démarrage avec
  Windows.
- **FR-018**: Le système DOIT refuser des réglages incohérents : seuil bas ≥ seuil haut, ou écart insuffisant pour
  que les seuils de fin d'alerte (± 2 %) ne se chevauchent pas ; le message DOIT indiquer la contrainte.
- **FR-019**: Chaque seuil DOIT pouvoir être remis à sa valeur par défaut d'un clic.
- **FR-020**: L'utilisateur DOIT pouvoir écouter chaque son au volume réglé via un bouton « Tester ».
- **FR-021**: Les réglages enregistrés DOIVENT être conservés entre les redémarrages ; s'ils sont absents ou
  illisibles, les valeurs par défaut DOIVENT être utilisées et l'utilisateur prévenu.
- **FR-022**: Quand « Démarrer avec Windows » est activé, l'application DOIT se lancer automatiquement à
  l'ouverture de la session, en arrière-plan, sans droits administrateur.

**Pause et snooze**

- **FR-023**: L'utilisateur DOIT pouvoir mettre en pause pour 15 min, 1 h ou indéfiniment, depuis la fenêtre
  principale et depuis le menu de l'icône.
- **FR-024**: Pendant une pause, le système DOIT continuer à suivre l'état réel de la batterie sans émettre
  d'alerte.
- **FR-025**: À la fin d'une pause ou sur « Reprendre », le système DOIT réévaluer l'état immédiatement et alerter
  si la condition est vraie.
- **FR-026**: L'état de pause (et son heure de fin) DOIT survivre à un redémarrage de l'application et être visible
  dans la fenêtre et sur l'icône.
- **FR-027**: Le snooze DOIT masquer la notification et couper le son de l'alerte en cours pendant 15 min, puis
  l'alerte DOIT reprendre si la condition est toujours vraie ; si l'alerte se termine
  pendant le snooze, le snooze est annulé.

**Présence et accès**

- **FR-028**: L'application DOIT afficher une icône dans la zone de notification, dont l'apparence distingue les
  états normal, alerte et pause.
- **FR-029**: Le clic gauche sur l'icône DOIT ouvrir ou masquer la fenêtre ; le clic droit DOIT proposer Ouvrir,
  Pause/Reprendre et Quitter.
- **FR-030**: Fermer la fenêtre principale NE DOIT PAS quitter l'application.
- **FR-031**: Le raccourci global `Ctrl+Alt+B` DOIT afficher la fenêtre principale au premier plan.
- **FR-032**: Une seule instance de l'application DOIT pouvoir tourner ; un second lancement DOIT afficher la
  fenêtre de l'instance existante.
- **FR-033**: La fenêtre principale DOIT afficher en permanence le niveau, l'état du secteur, l'état courant
  (normal, alerte basse, alerte haute, pause, snooze, aucune batterie) et l'heure de fin de la pause ou du snooze.

**Fiabilité et traçabilité**

- **FR-034**: L'application NE DOIT JAMAIS demander de droits administrateur.
- **FR-035**: Une erreur inattendue NE DOIT PAS fermer l'application silencieusement ; elle DOIT être journalisée et,
  si elle empêche les alertes, signalée à l'utilisateur.
- **FR-036**: L'application DOIT tenir un journal local consultable des événements importants : démarrage, arrêt,
  changements d'état (normal/alerte/pause), alertes émises, actions de l'utilisateur sur les notifications, erreurs.
- **FR-037**: Quand une application est en plein écran sur l'écran concerné, l'alerte DOIT être renforcée :
  elle DOIT interrompre la session en s'affichant devant l'application plein écran (en la faisant sortir du plein
  écran si nécessaire), dans un format agrandi et plus voyant que la notification normale, avec le son, et PEUT
  prendre le focus. Ce comportement n'est pas désactivable dans le MVP.
- **FR-038**: Aucun réglage NE DOIT permettre de limiter la durée ou le nombre de répétitions du son d'une alerte
  active : seuls la correction de la situation, le snooze et la pause l'interrompent.

### Key Entities *(include if feature involves data)*

- **État batterie** : instantané observé — niveau (%), secteur présent ou non, batterie en décharge ou non,
  batterie présente ou non, moment de l'observation.
- **État d'alerte** : NORMAL, ALERTE_BASSE, ALERTE_HAUTE, PAUSE (et le cas « aucune batterie ») ; inclut le moment
  de début de l'alerte et l'éventuelle fin de snooze.
- **Réglages** : seuil bas, seuil haut, son de l'alerte basse, son de l'alerte haute, volume,
  démarrage avec Windows, état de pause (fin de pause ou pause indéfinie).
- **Événement de journal** : horodatage, type d'événement, détail (ex. transition NORMAL → ALERTE_HAUTE à 80 %).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Dans 100 % des cas testés, une alerte apparaît au plus 10 s après que la batterie entre en zone
  d'alerte (y compris au réveil de veille).
- **SC-002**: Quand le niveau oscille de ± 1 % autour d'un seuil pendant 30 min, l'utilisateur reçoit au plus une
  déclenchement d'alerte.
- **SC-003**: Hors plein écran, aucun cas où la notification interrompt la saisie de l'utilisateur dans une autre
  application ; en plein écran, 100 % des alertes interrompent la session et sont vues par l'utilisateur.
- **SC-004**: 100 % des réglages et de l'état de pause sont restaurés à l'identique après un redémarrage.
- **SC-005**: Un nouvel utilisateur règle ses deux seuils et ses deux sons, puis les teste, en moins de 2 minutes.
- **SC-006**: Au repos, l'application ne cause aucun ralentissement perceptible : utilisation moyenne du processeur
  inférieure à 1 % et aucun impact visible sur l'autonomie mesurée sur une heure.
- **SC-007**: Les alertes cessent au plus 10 s après que l'utilisateur a corrigé la situation (branché/débranché).
- **SC-008**: Après une semaine d'usage, l'application n'a pas disparu de façon inattendue et chaque alerte émise
  est retrouvable dans le journal.

## Assumptions

- **Nom** : le produit s'appelle « BatNotif » pour le MVP (idee.md §11.1, renommage possible plus tard).
- **Cible** : un seul utilisateur Windows, un PC portable avec une seule batterie. Multi-utilisateurs et
  multi-batteries sont hors périmètre (idee.md §11.7).
- **Répétition / son** (idee.md §11.3 et §11.4, décidé) : notification persistante et son en boucle continue
  jusqu'à correction, snooze ou pause. Il n'y a donc plus d'intervalle de rappel réglable, ni de bouton « Vu », ni
  de fermeture automatique.
- **Pause** : choix de durée 15 min / 1 h / indéfiniment, comme dans la maquette de idee.md §6.1 (idee.md §11.5).
- **Snooze** : durée fixe de 15 min dans le MVP.
- **Hystérésis** : écart fixe de 2 % pour la fin d'alerte, non réglable dans le MVP.
- **Seuils réglables** de 5 % à 95 %, avec un écart minimum entre seuil bas et seuil haut suffisant pour respecter
  l'hystérésis.
- **Sons par défaut** : un son intégré pour chaque type d'alerte est fourni, utilisé tant que l'utilisateur n'a pas
  choisi de fichier et en repli en cas de fichier manquant.
- **Formats audio** pris en charge : au minimum MP3 et WAV.
- **Hors périmètre** (idee.md §4.3) : statistiques et historique, courbes de santé et cycles, notification vocale,
  coupure matérielle de la charge, thèmes de la notification.
- **Distribution** (idee.md §11.8) est une décision de réalisation, traitée au moment du plan.
- **Indépendance** (idee.md §10) : le produit est développé de zéro dans ce dépôt, sans rien reprendre d'un autre
  projet.
- **Source de référence** : idee.md ; toute décision prise ici qui tranche une question ouverte sera reportée dans
  idee.md.
