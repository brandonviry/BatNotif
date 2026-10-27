import { RootProvider } from 'fumadocs-ui/provider/next';
import type { Metadata, Viewport } from 'next';
import { Inter } from 'next/font/google';
import { appName, siteDescription, siteKeywords, siteTitle, siteUrl } from '@/lib/shared';
import './global.css';

const inter = Inter({
  subsets: ['latin'],
});

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: {
    default: siteTitle,
    template: `%s · ${appName}`,
  },
  description: siteDescription,
  keywords: siteKeywords,
  applicationName: appName,
  authors: [{ name: 'Brandon Viry', url: 'https://github.com/brandonviry' }],
  creator: 'Brandon Viry',
  category: 'software',
  alternates: { canonical: '/' },
  openGraph: {
    type: 'website',
    locale: 'fr_FR',
    siteName: appName,
    title: siteTitle,
    description: siteDescription,
    url: '/',
  },
  twitter: {
    card: 'summary_large_image',
    title: siteTitle,
    description: siteDescription,
  },
  robots: {
    index: true,
    follow: true,
    googleBot: { index: true, follow: true, 'max-image-preview': 'large', 'max-snippet': -1 },
  },
  formatDetection: { telephone: false },
  // Propriété Google Search Console (génère <meta name="google-site-verification" …>).
  verification: {
    google: 'QZ_eIyMR6Fze3QAJcCQcGwaFzbL3T-z4Cj6Y-B1PozM',
  },
};

export const viewport: Viewport = {
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#ffffff' },
    { media: '(prefers-color-scheme: dark)', color: '#0b0b0d' },
  ],
};

export default function Layout({ children }: LayoutProps<'/'>) {
  return (
    <html lang="fr" className={inter.className} suppressHydrationWarning>
      <body className="flex flex-col min-h-screen">
        <RootProvider>{children}</RootProvider>
      </body>
    </html>
  );
}
