# Site de documentation de BatNotif

Site [Fumadocs](https://fumadocs.dev) (Next.js) : page d'accueil + documentation utilisateur et développeur.

- Pages : `content/docs/*.mdx` (ordre et sections dans `content/docs/meta.json`)
- Page d'accueil : `app/(home)/page.tsx`
- SEO : `app/layout.tsx` (métadonnées), `app/sitemap.ts`, `app/robots.ts`, `app/opengraph-image.tsx`, données structurées `SoftwareApplication` sur l'accueil
- Déploiement : Vercel, relié au dépôt GitHub (dossier racine `docs`), à chaque push sur `main`

```powershell
pnpm install
pnpm build     # vérifie que le site compile
pnpm lint
```

L'URL publique est déduite de `VERCEL_PROJECT_PRODUCTION_URL` (fourni par Vercel) ou de `NEXT_PUBLIC_SITE_URL`.
