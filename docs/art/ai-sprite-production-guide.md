# DeskMate — Guide de production de sprites avec une IA

> **Statut : guide de travail, pas spécification figée.** Ce document sert à la fois de méthode de production pour un humain et de consignes à fournir à ChatGPT (ou à un autre assistant disposant d'un générateur d'images). Il complète [l'ADR 0003](../decisions/0003-layered-sprite-animation.md). Il ne prescrit pas encore un atlas, une résolution ou un format de manifeste définitifs.

## 1. Objectif

Obtenir, à partir d'une photographie ou d'un portrait de référence, **un même personnage 2D cohérent sur la durée** : mêmes traits, proportions, tenue, palette, contours et lumière, malgré de nouvelles poses, expressions et animations.

Ce personnage sera utilisé comme **marionnette graphique de DeskMate**, et non comme une illustration unique. Il doit être découpable et animable en **cinq couches indépendantes** :

1. **Corps** : posture, locomotion, gestes.
2. **Tête** : orientations, inclinaisons, mouvements.
3. **Yeux** : regard, clignements, expressions.
4. **Bouche** : expressions et visèmes pour la parole.
5. **Accessoires** : objets et éléments de contexte.

Les couches se composent à l'aide de **points d'ancrage**, de pivots et d'un ordre de superposition défini. La cohérence **géométrique** compte autant que la cohérence **stylistique**.

**Principe :** ne pas demander à l'IA d'improviser un nouveau personnage à chaque génération. On constitue une référence canonique, puis on génère des variations contrôlées à partir d'assets déjà acceptés.

## 2. Quel outil employer ?

**Choix de départ : ChatGPT pour la génération et l'édition des images.** Ses fonctionnalités de génération d'images et d'utilisation de références visuelles en font un bon candidat pour des itérations supervisées. **Claude ou ChatGPT** conviennent pour formaliser les demandes, aider à contrôler les fichiers, écrire des scripts, faire de la revue et organiser le pipeline.

Ce n'est **ni une garantie de fidélité parfaite ni un choix irréversible**. Si un autre générateur fournit de meilleurs résultats sur une série de tests concrets, le workflow reste identique. Une image satisfaisante n'implique pas que les assets soient directement exploitables en production.

### Ne pas confondre les responsabilités

| Travail | Responsable |
| --- | --- |
| Choisir les références et accepter les résultats | Humain |
| Proposer des variations visuelles | Générateur d'images IA |
| Contrôler dimensions, transparence, ancrages et cohérence | Humain + contrôles automatisables |
| Découper, nettoyer et exporter des assets | Outils graphiques / scripts, avec éventuelle assistance IA |
| Composer et animer les cinq couches | Moteur DeskMate |

## 3. Références disponibles pour le personnage initial

Les exemples fournis lors des échanges sont :

- **Photographie/portrait d'origine** : référence d'identité, pas forcément référence de style final.
- **Portrait stylisé** : référence du visage et de la direction artistique.
- **Personnage entier en pose neutre** : référence principale du corps, des vêtements et des proportions.
- **Planche de poses du corps** : référence de gestuelle et de silhouette.
- **Planche d'orientations de la tête** : référence de vues et d'angles.

**Important :** les planches fournies sur fond magenta ne constituent **pas** des PNG à fond transparent exploitables tels quels. Il faudra obtenir ou produire une véritable couche alpha, avec des bords propres. Les planches initiales sont des **références visuelles**, pas encore des sprites modulaires certifiés.

Les images partagées dans une conversation ne sont **pas automatiquement disponibles** dans une nouvelle conversation ou un nouveau projet ChatGPT : il faut les joindre aux instructions lors de la génération. **Ne pas publier une photo personnelle dans un dépôt GitHub public sans décision explicite.**

## 4. Créer une « bible visuelle » et un personnage maître

Avant de multiplier les poses, valider un **modèle canonique** comprenant :

- **Full body de face, debout, neutre**, cadré entièrement.
- **Portrait du visage neutre** dans le style final.
- **Tenue et accessoires permanents** clairement définis.
- **Palette de couleurs, contours, ombres et niveau de détail** cohérents.
- **Proportions de référence** : taille de la tête relativement au corps, largeur des épaules, mains, jambes, pieds.

Le personnage illustré dans les références actuelles possède notamment des cheveux courts foncés, une barbe/moustache stylisée, un pull beige/gris clair sur chemise blanche, un pantalon foncé et des chaussures brunes. Ces éléments sont des **observations de références à confirmer lors de la validation du maître**, pas des valeurs codées en dur dans le moteur.

Une fois le maître approuvé, il devient une **source de vérité pour l'identité et le style**. Toute nouvelle génération doit être confrontée au maître. Une modification délibérée (nouvelle tenue, autre style, retouche du visage) doit donner lieu à une nouvelle version des références, et non à une dérive silencieuse.

### Références d'ancrage (« anchors »)

Créer et approuver progressivement :

- Face neutre du personnage complet.
- Tête de face, trois-quarts gauche/droite, profil gauche/droite, regard haut/bas.
- Quelques poses corporelles représentatives.
- Expressions de référence (neutre, content, perplexe).
- Bouches/visèmes de référence.

La **photo source** sert à préserver les traits reconnaissables ; le **maître stylisé** prime pour la forme et la direction artistique ; les **assets approuvés** priment pour les dimensions, alignements et détails déjà verrouillés.

## 5. Ordre de production conseillé

Produire en lots **petits et vérifiables**, plutôt qu'en une seule demande de 80 sprites.

### Lot 1 — Références canoniques

Maître complet neutre, portrait stylisé, palette et guide de proportions. **Point de validation obligatoire** avant le reste.

### Lot 2 — Tête

Face, trois-quarts et profils, inclinaison vers le haut et vers le bas. Garder le même point d'attache au cou. Attention : un angle important peut nécessiter **une variante complète de la tête**, pas une simple rotation du sprite de face.

### Lot 3 — Yeux

Ouverts, demi-fermés, fermés, clignement, surprise, joie, scepticisme, regards gauche/droite. Définir leur point d'ancrage **dans la tête**.

### Lot 4 — Bouche et visèmes

Au minimum : repos/fermée, ouverture faible, grande ouverture, lèvres arrondies (« O/OU »), lèvres fermées (« M/B/P »), « F/V », sourire. **Le jeu exact de visèmes n'est pas figé** ; il dépendra des moteurs TTS et de la lisibilité graphique.

Prévoir un emplacement de bouche cohérent, et si besoin une zone de masquage pour qu'une bouche interchangeable ne laisse pas apparaître une seconde bouche dessinée sur la tête.

### Lot 5 — Corps et gestes

Idle, marche, course éventuelle, saut, salut, réflexion, travail, succès, erreur, positions assises ou accroupies si souhaitées. Définir les articulations utiles et la place de la tête sur chaque pose.

### Lot 6 — Accessoires

Bulles, pancartes, tasse, loupe, microphone, ordinateur, objets contextuels. Ils ne sont pas tous obligatoires ; ne les générer que lorsqu'une interaction réelle en a besoin.

### Lot 7 — Assemblage et animations

Assembler les couches approuvées dans le moteur et vérifier les séquences (idle, clignement, parole, marche, etc.). Une sprite sheet finale peut être construite **après** validation des images élémentaires ; le format d'atlas ne doit pas dicter la phase créative.

## 6. Contraintes pour des assets réellement réutilisables

- **PNG avec canal alpha véritable**, lorsque le résultat est destiné au runtime ; aucun arrière-plan magenta ou blanc peint derrière le sprite.
- **Pas de texte, bordure, ombre externe parasite, watermark ou décor** inclus accidentellement dans un sprite de personnage.
- **Cadre de travail stable** : même canevas et mêmes repères pour les frames comparables, même origine pour les ancrages.
- **Personnage intégralement dans le cadre** : aucun pied, bras, cheveu ou accessoire coupé involontairement.
- **Contours et anti-crénelage propres**, sans halo magenta après extraction de fond.
- **Ordre des couches documenté** (un accessoire peut passer devant/derrière le corps ou la tête selon l'action).
- **Ancrages explicites** : cou, tête, yeux, bouche, pieds et accessoires selon le besoin.
- **Aucune variation non demandée** de vêtements, de visage, de palette, de style ou de proportions.
- **Pas de fausse modularité** : une planche d'images de personnages complets ne remplace pas des couches effectivement séparées.
- Un asset peut demander une **retouche manuelle** et des tests de composition ; les images générées ne sont pas présumées « production-ready ».

Pour une animation, imposer aussi une **cadence cohérente**, la continuité des détails d'une frame à l'autre et l'absence de « morphing » accidentel. Les nombres de frames et dimensions d'atlas seront décidés plus tard, en fonction du prototype.

## 7. Organisation recommandée des assets

Proposition à ajuster selon le projet :

```text
assets/
  characters/
    <character-id>/
      README.md                 # identité, style, version, droits
      references/               # maîtres stylisés validés
        master-front.png
        master-face.png
        turnaround.png
      working/                  # essais non validés, retouches
      approved/
        body/
        head/
        eyes/
        mouth/
        accessories/
      animations/               # définition des séquences, atlases générés
      metadata/                 # ancrages, pivots, dimensions, ordre de couches
```

**Attention à la confidentialité :** conserver la photographie originale dans un espace privé ou local, hors du dépôt public, sauf consentement explicite. Les références stylisées ne doivent pas non plus être publiées automatiquement : vérifier les droits et le caractère personnel de chaque image.

Chaque asset approuvé devrait indiquer **son rôle, son orientation, sa version, ses dimensions et ses références d'origine**. Les noms devraient être stables, explicites et déterministes, par exemple `head-front-neutral.png`, `eyes-front-blink-02.png`, `mouth-front-viseme-o.png`.

Une métadonnée d'ancrage pourra utiliser un JSON simple, mais **son schéma n'est pas décidé**. Ne pas figer un contrat runtime à partir des exemples de prompts.

## 8. Workflow d'une génération

1. **Choisir un seul objectif concret** : une pose, un angle, une expression, un visème, un accessoire ou un petit lot homogène.
2. **Joindre les bonnes références** : maître stylisé + références déjà validées pertinentes ; ajouter la photo source seulement si nécessaire à l'identité.
3. **Spécifier les invariants** : traits, proportions, couleurs, vêtement, taille du canevas, points d'ancrage et transparence.
4. **Générer/éditer**, de préférence en modifiant une référence validée plutôt qu'en recréant le personnage.
5. **Contrôler visuellement**, côte à côte avec les références ; détecter toute dérive.
6. **Contrôler techniquement** : transparence, dimensions, cadrage, découpage, ancrages, continuité.
7. **Corriger, rejeter ou approuver**. Ne pas faire d'un brouillon non validé une référence canonique.
8. **Archiver la version approuvée** et la joindre comme référence à la prochaine génération.

Ne jamais déclarer qu'une image est conforme « à 100 % » sans inspection. Une IA peut aider à la revue, mais ne remplace pas l'acceptation humaine ni les validations dans le moteur.

## 9. Utiliser ce même document avec ChatGPT

Un **seul document** suffit pour transmettre la méthode et les règles de production. Il n'est pas nécessaire de développer immédiatement un « skill » ou une usine de génération.

### Configuration pratique

1. Créer un **projet ChatGPT** dédié, par exemple **« DeskMate — Sprite Factory »**.
2. Ajouter **ce fichier Markdown** aux fichiers/instructions du projet et fournir les références visuelles approuvées.
3. Ajouter aux instructions du projet la phrase : « Pour toutes les générations de sprites DeskMate, applique le document *Guide de production de sprites avec une IA*. Les assets validés sont prioritaires sur les improvisations. »
4. Pour chaque demande, **joindre ou identifier explicitement les références nécessaires**. Le document seul ne transporte pas les pixels des images.
5. Si le résultat est accepté, l'intégrer au pack de références et indiquer son statut. Si refusé, décrire précisément ce qui doit être corrigé.

**Limite importante :** le seul fait que ce fichier existe sur GitHub ne signifie pas que ChatGPT le lira automatiquement dans chaque conversation. Il doit être accessible dans le contexte utilisé (projet, pièce jointe, ou outil capable d'accéder au dépôt). De même, une « instruction persistante » ne garantit pas une identité visuelle rigoureusement stable : c'est le **workflow de références et de vérification** qui limite la dérive.

### Instructions à appliquer par l'IA (mode opératoire)

Lorsqu'on te demande de générer un asset pour DeskMate :

- **Lis d'abord les règles de ce document** et relève les références canoniques présentes.
- Identifie **l'asset exact, sa couche, son état, sa vue et son format de sortie**. Si les précisions ne sont pas disponibles, évite les suppositions qui changeraient l'identité ou l'architecture.
- Préserve strictement **identité, style, proportions, tenue, palette, contours, éclairage et points d'ancrage** des références validées.
- Génère **uniquement les variations demandées**. N'ajoute pas de nouveaux vêtements, objets, expressions ou détails gratuits.
- Préfère une **édition contrôlée d'un asset approuvé** à une recréation libre.
- Ne confonds pas **image de démonstration**, **sprite de couche séparée** et **asset prêt pour le runtime**.
- Ne présume pas qu'un fond uni est transparent ; vérifie l'alpha si un contrôle de fichier est disponible.
- Après la génération, signale **les points restant à vérifier** et toute difficulté visible ; n'invente pas d'inspection automatique si elle n'a pas eu lieu.
- **Ne modifie pas les références canoniques** ni les assets approuvés sans décision explicite.
- Considère la cohérence entre les frames et la compatibilité avec les **cinq couches indépendantes** comme prioritaires sur une illustration séduisante mais inutilisable.

## 10. Prompt maître réutilisable

À utiliser en joignant les références validées. Remplacer les champs entre crochets :

> Tu es le directeur artistique et l'outil de production d'assets 2D pour **DeskMate**, une marionnette animée sur Windows.  
> Ton objectif est de produire **[asset demandé]**, pour la couche **[body/head/eyes/mouth/accessories]**, vue **[angle]**, état **[état]**.  
> **Références prioritaires :** [maître stylisé], [assets approuvés pertinents], et si nécessaire [photo source].  
> Préserve sans réinterprétation l'identité visuelle du personnage, sa tenue, ses proportions, ses couleurs, ses contours et son rendu cartoon.  
> Produis uniquement la couche demandée, avec un cadrage et des points d'ancrage compatibles avec les références ; ne redessine pas les autres couches si elles ne sont pas demandées.  
> **Sortie souhaitée :** PNG RGBA à fond transparent, élément entier dans le cadre, taille/repères [valeurs si fixées], pas de texte ni de décor.  
> **Changements autorisés :** [liste précise]. **Éléments invariants :** [liste précise].  
> Ne rajoute rien et ne change pas le style. Si une contrainte est impossible à respecter fidèlement, indique-le plutôt que de prétendre que l'asset est parfaitement réutilisable.

### Exemple de demande ciblée

> À partir du maître validé et de la tête de face, produis **la bouche seule correspondant à un « O » arrondi**, sans nez, yeux, menton ni arrière-plan. Conserve l'épaisseur des contours et la palette validée. Place-la sur le même canevas local et au même point d'ancrage que les autres bouches. Un seul visème ; ne change rien au visage de référence.

Pour une pose corporelle, remplacer par une instruction explicite portant sur **le corps, la pose, les attaches au cou et les membres**, en interdisant toute variation du visage ou de la tenue.

## 11. Vérification et critères d'acceptation

### Contrôle visuel

- [ ] Personnage immédiatement reconnaissable et fidèle au maître.
- [ ] Même style de dessin : contours, aplats, ombrage, palette.
- [ ] Même tenue et proportions, sauf variation explicitement autorisée.
- [ ] Vue, pose et expression conformes à la demande.
- [ ] Aucune anomalie anatomique, élément supplémentaire ou déformation entre frames.

### Contrôle technique

- [ ] Vrai canal alpha et absence de fond/halo.
- [ ] Canevas, cadrage et échelle conformes au lot.
- [ ] Points d'ancrage reproductibles et cohérents.
- [ ] Couche réellement indépendante et composable avec les autres.
- [ ] Frame(s) complètes, pas de membre coupé ni de décalage parasite.
- [ ] Transparence et superposition vérifiées dans un aperçu de composition, idéalement dans DeskMate.
- [ ] Nom, rôle, version et statut d'approbation identifiables.

**Refuser ou retravailler** tout asset qui échoue sur l'identité, la géométrie, la modularité ou la transparence, même s'il est joli. Le moteur aura besoin de **cohérence et de reproductibilité**, pas seulement de qualité artistique.

## 12. Ce que nous ne décidons pas encore

- Résolution exacte des sprites, dimensions des sprite sheets et fréquence d'animation.
- Schéma des manifests/metadata, stratégie d'atlas et optimisation mémoire.
- Nombre définitif d'angles, de poses, d'expressions et de visèmes.
- Outil de détourage et corrections manuelles automatisées ou non.
- Générateur d'images définitif, usage éventuel d'autres modèles spécialisés.
- Articulation précise entre pré-rendus de têtes orientées, pivots 2D et masquage des yeux/de la bouche.

Ces décisions doivent être guidées par **une première preuve de concept** : une pose debout, une tête et des yeux distincts, quelques bouches, une animation idle et une courte phrase prononcée avec une bouche synchronisée.

---

**Résumé opérationnel :** établir un **maître**, produire des **références d'ancrage**, générer des **petits lots par couche**, **valider** chaque résultat puis seulement **assembler/exporter**. Le prompt et ce document cadrent le travail de l'IA ; les images canoniques et la validation restent la source de vérité.
