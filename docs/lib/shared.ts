import { createGetUrl } from 'fumadocs-core/source';

export const appName = 'BatNotif';
export const docsRoute = '/docs';
export const docsImageRoute = '/og/docs';
export const docsContentRoute = '/llms.mdx/docs';

export const siteTitle = 'BatNotif — alerte batterie pour préserver la santé de la batterie de ton PC portable';
export const siteDescription =
  "BatNotif est une application Windows gratuite qui protège la santé de la batterie de ton PC portable : " +
  "notification et son en boucle quand il faut brancher (batterie faible) ou débrancher (batterie à 80 %) le chargeur.";

export const siteKeywords = [
  'batterie',
  'notification batterie',
  'alerte batterie',
  'santé batterie',
  'santé de la batterie',
  'batterie PC portable',
  'préserver la batterie',
  'charge 80 %',
  'batterie 20 80',
  'débrancher le chargeur',
  'batterie faible',
  'batterie lithium-ion',
  'Windows 11',
  'Windows 10',
  'BatNotif',
];

export const gitConfig = {
  user: 'brandonviry',
  repo: 'BatNotif',
  branch: 'main',
};

export const repoUrl = `https://github.com/${gitConfig.user}/${gitConfig.repo}`;
export const releasesUrl = `${repoUrl}/releases/latest`;
export const downloadUrls = {
  standard: `${repoUrl}/releases/latest/download/BatNotif-standard.exe`,
  compact: `${repoUrl}/releases/latest/download/BatNotif-compact.exe`,
};

/**
 * URL publique du site : NEXT_PUBLIC_SITE_URL si défini, sinon le domaine de production
 * fourni automatiquement par Vercel, sinon localhost en développement.
 */
export const siteUrl = (
  process.env.NEXT_PUBLIC_SITE_URL ??
  (process.env.VERCEL_PROJECT_PRODUCTION_URL
    ? `https://${process.env.VERCEL_PROJECT_PRODUCTION_URL}`
    : 'http://localhost:3000')
).replace(/\/$/, '');

const getContentUrl = createGetUrl(docsContentRoute);

export function getPageMarkdownUrl(page: { slugs: string[]; locale?: string }) {
  const segments = [...page.slugs, 'content.md'];

  return { segments, url: getContentUrl(segments, page.locale) };
}

const getImageUrl = createGetUrl(docsImageRoute);

export function getPageImageUrl(page: { slugs: string[]; locale?: string }) {
  const segments = [...page.slugs, 'image.png'];

  return { segments, url: getImageUrl(segments, page.locale) };
}
