import type { MetadataRoute } from 'next';
import { source } from '@/lib/source';
import { siteUrl } from '@/lib/shared';

export default function sitemap(): MetadataRoute.Sitemap {
  const lastModified = new Date();

  return [
    { url: `${siteUrl}/`, lastModified, changeFrequency: 'monthly', priority: 1 },
    ...source.getPages().map((page) => ({
      url: `${siteUrl}${page.url}`,
      lastModified,
      changeFrequency: 'monthly' as const,
      priority: page.url === '/docs' ? 0.9 : 0.7,
    })),
  ];
}
