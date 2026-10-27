import type { BaseLayoutProps } from 'fumadocs-ui/layouts/shared';
import { appName, releasesUrl, repoUrl } from './shared';

function Logo() {
  return (
    <span className="inline-flex items-center gap-2 font-semibold">
      <svg width="22" height="22" viewBox="0 0 32 32" aria-hidden="true">
        <circle cx="16" cy="16" r="15" fill="#2E9E5B" />
        <rect x="9" y="11" width="13" height="10" rx="2" fill="none" stroke="#fff" strokeWidth="2" />
        <rect x="22.5" y="14" width="2" height="4" rx="1" fill="#fff" />
        <rect x="11" y="13" width="6" height="6" rx="1" fill="#fff" />
      </svg>
      {appName}
    </span>
  );
}

export function baseOptions(): BaseLayoutProps {
  return {
    nav: {
      title: <Logo />,
      url: '/',
    },
    githubUrl: repoUrl,
    links: [
      { text: 'Documentation', url: '/docs', active: 'nested-url' },
      { text: 'Télécharger', url: releasesUrl, external: true },
    ],
  };
}
