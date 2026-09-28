# Réception logistique

Application web permettant à un magasinier de consulter une commande fournisseur en attente et d'en
valider la réception à trois niveaux : palette, carton, produit. Test technique full stack
(React / .NET) — environ une journée de travail.

**Stack :** .NET 10 (ASP.NET Core, EF Core) · PostgreSQL 17 · Next.js 16 / React 19 / TypeScript ·
Docker Compose.

## Lancer le projet

Prérequis : **Docker** (Docker Desktop ou Engine + Compose v2). Rien d'autre.

```bash
git clone <url-du-depot> && cd <dossier>
docker compose up --build
```

Quand les trois services sont `healthy` (1 à 2 minutes au premier lancement) :

| Quoi | Où |
|---|---|
| Application | http://localhost:3000 |
| Documentation de l'API (Scalar) | http://localhost:5080/scalar/v1 |
| Santé de l'API | http://localhost:5080/health |

La base est migrée et alimentée au premier démarrage avec la commande de démonstration `CMD-2026` et un compte opérateur.

**Connexion :** compte de démonstration **`magasinier` / `Reception2026`**.
Utilisez `http://localhost:3000` ou `http://127.0.0.1:3000` : tout autre nom d'hôte est refusé par la protection CSRF.
Les données survivent à `docker compose down` ; pour repartir de zéro : `docker compose down -v`.
Un port déjà pris ? Copier `.env.example` en `.env` et changer `WEB_PORT`, `API_PORT` ou `POSTGRES_PORT`.

> Le mot de passe PostgreSQL, le compte de démonstration et la clé de signature des sessions de
> `docker-compose.yml` sont des valeurs **jetables pour une démonstration locale** (conteneurs liés à
> `127.0.0.1`). Pour tout autre usage : `POSTGRES_PASSWORD` et `JWT_SECRET` dans un fichier `.env`. L'API
> refuse de démarrer avec une clé de moins de 32 octets. Aucun secret réel n'est versionné.

### Développer sans conteneur applicatif

```bash
docker compose up -d postgres                                   # base seule
dotnet run --project MS.SS.Core/MS.SS.Core.API                  # API sur http://localhost:5080
cd MS.CA.ClientApp && cp .env.example .env.local && npm ci && npm run dev   # UI sur http://localhost:3000
```
Prérequis : .NET SDK 10, Node 22.

### Tests

```bash
dotnet test MS.SS.Core/MS.SS.Core.slnx      # nécessite Docker (PostgreSQL éphémère via Testcontainers)
cd MS.CA.ClientApp && npm run check         # typecheck, lint, format, tests, build
```

## Ce qui est demandé, et comment c'est couvert

| Besoin du sujet | Réalisation |
|---|---|
| Se connecter | Page de connexion en français ; session par cookie `HttpOnly` ; **toutes** les routes de réception exigent une session |
| Voir la commande sans surcharge | Arbre Palette → Carton → Produit **replié par défaut** ; on déplie niveau par niveau ; en-tête fixe avec la jauge |
| Valider à 3 niveaux | Case à cocher à chaque niveau ; valider un parent valide tous ses descendants |
| Remontée automatique | Le statut d'un carton/palette est **calculé** à partir de ses produits : tous cochés → validé ; un décoché → « partiel » |
| Suivi global | Jauge « X / Y articles reçus » toujours visible |
| API REST fonctionnelle et structurée | 6 endpoints documentés (OpenAPI/Scalar), erreurs `ProblemDetails` avec code stable |
| Tests | domaine, cas d'usage, persistance et API (PostgreSQL réel), composants et hooks côté front |

## Architecture

### Vue d'ensemble

```
navigateur ──► web (Next.js :3000) ──/api/core/*──► api (ASP.NET Core :8080) ──► PostgreSQL 17
                                        (rewrite)                                 schéma « reception »
```
Le navigateur n'appelle que sa propre origine ; Next.js relaie `/api/core/*` vers l'API. Pas de CORS à
gérer, pas d'URL d'API dans le code des composants.

### Back-end (`MS.SS.Core`) — monolithe modulaire

| Projet | Rôle |
|---|---|
| `Modules.Reception` | Contexte métier de la réception : `Domain` (agrégat), `Application` (cas d'usage), `Infrastructure` (EF Core) |
| `Modules.Identity` | Contexte des comptes opérateurs : connexion, inscription, session |
| `Security` | Hachage des mots de passe (PBKDF2), jetons JWT, politique d'autorisation |
| `Infrastructure` | `AppDbContext`, registre des modules et de leurs **schémas PostgreSQL**, migrations |
| `SharedKernel` | `Result`, exceptions codées, validateurs, `Entity` |
| `App` | Composition : injection de dépendances, CORS, health checks, données de démo |
| `API` | Endpoints Minimal API (un fichier par groupe, une méthode privée par endpoint), gestion d'erreurs, OpenAPI |

- **Domain-Driven Design, à l'échelle du sujet.** `Delivery` est la racine d'agrégat (Livraison →
  Palette → Carton → Ligne produit). Toute modification passe par elle, et elle porte les invariants.
- **Un schéma PostgreSQL par module** (`identity`, `reception`). Chaque module possède ses tables, sans clé
  étrangère entre schémas ; `Reception` ne dépend pas d'`Identity` dans le code, il est protégé par la
  politique d'autorisation. Ajouter un contexte (par exemple `ordering`, la création de commandes exclue du
  sujet) = un module, un schéma, une ligne dans le registre.
- **Cas d'usage** exposés par des handlers (Wolverine, en mémoire) : l'endpoint ne contient aucune logique,
  le handler ne contient que de l'orchestration, la règle métier est dans le domaine.
- **Gestion d'erreurs unique** : un handler renvoie un `Result` ; un seul endroit le transforme en statut
  HTTP et en `code` stable (le front traduit à partir du code).

### Authentification et sécurité

- **Mots de passe** : PBKDF2-HMAC-SHA256, 600 000 itérations, sel aléatoire, hachage auto-descriptif
  (le coût peut augmenter sans invalider les comptes). Politique : 8 caractères minimum avec majuscule,
  minuscule et chiffre, publiée par l'API (`/password-policy`) pour que le front ne la duplique pas.
- **Session** : un JWT de 8 heures dans un cookie `HttpOnly`, `SameSite=Lax` (`Secure` hors développement).
  Le jeton n'apparaît jamais dans une réponse et n'est pas lisible par JavaScript ; le front interroge
  `/me` pour savoir s'il existe une session.
- **Fermé par défaut** : une politique d'autorisation globale exige un utilisateur authentifié ; seuls la
  connexion, l'inscription, la politique de mot de passe, la santé et la documentation de l'API sont publics.
  Un nouvel endpoint oublié est donc protégé, pas ouvert.
- **Un seul message d'échec** à la connexion (compte inconnu, mauvais mot de passe, compte inactif) et un
  temps de calcul équivalent quand le compte n'existe pas.
- **CSRF** : toute écriture portant le cookie doit venir d'une origine autorisée et porter l'en-tête
  `X-MS-CSRF`. **Limitation de débit** : 10 tentatives par minute et par client réel (`X-Forwarded-For`),
  pour qu'un opérateur qui se trompe ne bloque pas les autres.
- **Limites assumées** : pas de jeton de rafraîchissement ni de révocation côté serveur (« Se déconnecter »
  supprime le cookie, le jeton reste valide jusqu'à son expiration) ; un seul rôle ; pas d'e-mail ni de
  réinitialisation de mot de passe.

### Front-end (`MS.CA.ClientApp`)

Organisation `api/` (seul point d'appel réseau) · `core/` (erreurs, textes français) · `features/`
(tranche « reception ») · `shared/` (composants génériques, accessibles, sans bibliothèque UI) ·
`layout/` · `app/` (routes). TanStack Query gère l'état serveur. La tranche `auth` fournit le fournisseur de
session, la garde de routes (redirection vers `/login?returnTo=…`, adresse de retour validée) et la page de
connexion ; à l'expiration de la session, tout le cache est vidé.

### Modèle de données

```
identity.users  (username unique, password_hash, display_name, role, is_active, last_login_at)

reception.deliveries ─┬─< reception.pallets ─┬─< reception.cartons ─┬─< reception.product_lines
   order_number (unique)   code, position        code, position        reference, name, color, size,
                                                                       expected_quantity, received_quantity
```
`CHECK (received_quantity BETWEEN 0 AND expected_quantity)` : la base garantit la borne même si le code
la contournait. **Aucune colonne de statut** : voir ci-dessous.

## Règles métier

- **Une seule donnée est stockée : la quantité reçue par ligne produit** (entier, `0 ≤ reçue ≤ attendue`).
- Le statut d'une ligne, d'un carton, d'une palette et de la livraison est **dérivé** de ces quantités :
  `none` (rien reçu), `all` (tout reçu), `partial` (entre les deux). Il n'est jamais stocké : impossible
  d'avoir un carton « validé » avec un produit non reçu.
- Valider un nœud = mettre chaque produit en dessous à sa quantité attendue ; le décocher = remettre à 0.
- **Le serveur est l'unique propriétaire de ces règles.** Le front affiche ce que l'API renvoie et ne
  recalcule jamais un statut.

## Choix et zones d'ombre

| Question | Décision | Pourquoi |
|---|---|---|
| Saisie des quantités | Cocher = tout reçu ; on peut aussi **saisir une quantité partielle** (entier, de 0 à l'attendu) | Couvre le cas nominal (case à cocher) et le cas réel (livraison incomplète) avec un seul modèle |
| Sauvegarde à chaque clic ou à la fin | **À chaque action**, sans étape de confirmation ; l'interface attend la réponse du serveur | L'état survit à un rechargement ou à un second poste ; la cascade reste calculée côté serveur |
| Que compte « X / Y articles » | Des **unités** (somme des quantités) et non des lignes | Le sujet parle d'« articles » et fournit des quantités |
| Décocher une palette/un carton | Autorisé : remet tout en dessous à 0 | Une erreur de manipulation doit être réversible |
| Valider un carton déjà partiellement reçu | Le **complète** (n'inverse pas) | Comportement prévisible et idempotent |
| Carton ou palette vide | Statut « non reçu », jamais « reçu » | Une vérité « à vide » ne doit pas passer pour une réception |
| Concurrence entre deux magasiniers | Pas de verrou ; dernière écriture gagnante par ligne. Les statuts étant dérivés, aucun état incohérent n'est possible ; l'interface se resynchronise au retour sur l'onglet | Périmètre d'une journée ; limite assumée |
| « Commande en cours » | La plus ancienne livraison | Une seule est fournie en données de démo |
| Optimisme côté interface | **Aucune mise à jour optimiste** de la cascade : l'interface affiche la réponse du serveur, les requêtes d'un même écran sont sérialisées | Évite de dupliquer la règle métier côté client et tout mécanisme de rollback |
| Forme du JSON | `id` + `code` (ex. `PAL-01`) à chaque niveau, plus `status`, `progress` et `receivedQuantity` | L'exemple du sujet est indicatif ; `palletId: "PAL-01"` n'est pas un identifiant |
| Authentification | Connexion par **identifiant** + mot de passe, cookie de session `HttpOnly` de 8 h, aucune donnée utilisateur stockée côté réception | Le sujet n'en demande pas ; le besoin exprimé est un accès protégé, pas un historique nominatif |
| Inscription | **Optionnelle, côté API seulement** (`POST /register`, testé) ; un compte de démonstration est créé au démarrage. La page d'inscription n'a pas été construite, faute de temps | L'endpoint est peu coûteux et retirable ; la page était la partie « Should » du périmètre, sacrifiée en premier |
| Rafraîchissement de session | Aucun : un jeton de 8 h (une journée de travail) | Évite une table de jetons et un protocole de rotation disproportionnés pour ce périmètre |
| Base de données | PostgreSQL réel (et non en mémoire), schéma par module | Les tests d'intégration exercent contraintes et cascades réelles |
| Bibliothèque de composants | Aucune ; composants accessibles écrits à la main | Poids et dépendances inutiles pour cet écran |

## API

**Toutes les routes de réception exigent une session** (sinon `401`, code `auth.unauthenticated`). Documentation
interactive sur `/scalar/v1`.

Authentification — préfixe `/api/identity/auth` :

| Méthode | Chemin | Rôle |
|---|---|---|
| POST | `/sign-in` | `{"username","password"}` → pose le cookie de session ; `401 auth.invalid_credentials` pour tout échec |
| POST | `/register` | Crée un opérateur et ouvre la session (accessible par l'API, pas de page dédiée) |
| POST | `/sign-out` | Supprime le cookie |
| GET | `/me` | Session courante |
| GET | `/password-policy` | Règles de mot de passe |

Réception — préfixe `/api/reception/deliveries` :

| Méthode | Chemin | Rôle |
|---|---|---|
| GET | `/current` | La commande en cours, statuts et progression inclus |
| GET | `/{id}` | Une commande par identifiant |
| PUT | `/{id}/pallets/{palletId}/validation` | `{"validated": true \| false}` |
| PUT | `/{id}/cartons/{cartonId}/validation` | idem |
| PUT | `/{id}/products/{productId}/validation` | idem |
| PUT | `/{id}/products/{productId}/received-quantity` | `{"receivedQuantity": 12}` |

Chaque écriture est idempotente et renvoie la commande complète mise à jour. Les erreurs sont des
`application/problem+json` avec un champ `code` stable (`reception.quantity_out_of_range`, …) ; un
identifiant qui n'appartient pas à la commande de l'URL répond `404`.

## Tests

597 tests passent (355 côté API, 242 côté client), tous exécutés contre une vraie base PostgreSQL — aucun
n'utilise un fournisseur en mémoire.

- **Sécurité et identité** (103 tests) : hachage et jetons (expirés, falsifiés, mauvais émetteur ou
  audience), échec de connexion indiscernable, cookie `HttpOnly`, CSRF, limitation de débit par client,
  course à l'inscription, secret trop court.
- **Domaine** (36 tests, sans base) : cascade descendante, remontée, décochage → partiel, bornes,
  idempotence, identifiants inconnus ou du mauvais type, entités vides.
- **Cas d'usage et persistance** (PostgreSQL éphémère via Testcontainers) : rien n'est enregistré sur un
  chemin d'erreur ; schéma, aller-retour de l'agrégat, contrainte `CHECK`, suppression en cascade, unicité.
- **API** (HTTP réel + PostgreSQL) : les trois user stories de bout en bout, corps invalides, requêtes
  concurrentes, format JSON, document OpenAPI, isolation CORS/CSRF.
- **Front** (242 tests) : erreurs, client HTTP (en-tête CSRF, 401), session (restauration, déconnexion qui
  vide le cache, expiration), garde de routes, adresse de retour (redirections ouvertes refusées),
  formulaire de connexion, case à trois états, arbre, saisie de quantité, page (chargement, vide,
  hors-ligne, échec d'enregistrement).

## Limites et pistes

- Pas d'historique nominatif des actions du magasinier (la réception ne stocke aucune donnée utilisateur) ; un seul rôle ; pas de révocation de session côté serveur.
- Un second contexte `ordering` (création de commandes) se brancherait comme un nouveau module et un
  nouveau schéma, avec une API de contrat entre les deux.
- Verrouillage optimiste (`xmin`) si plusieurs magasiniers travaillent réellement en parallèle.
- Tests de bout en bout navigateur (Playwright) : non faits, la chaîne est couverte par les tests
  d'API et de page.
- Page d'inscription : non construite (voir « Choix et zones d'ombre » ci-dessus), l'API est prête et testée.
- Limitation de débit à la connexion : un client peut forger un `X-Forwarded-For` non repassé par le proxy
  Next.js pour contourner son propre plafond ; sans conséquence sur les données, seulement sur ce plafond.
