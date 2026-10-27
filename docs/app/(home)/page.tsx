import type { Metadata } from 'next';
import Link from 'next/link';
import {
  BatteryCharging,
  BatteryWarning,
  BellRing,
  Download,
  Gamepad2,
  Keyboard,
  Music,
  PauseCircle,
  ShieldCheck,
} from 'lucide-react';
import { appName, downloadUrls, releasesUrl, repoUrl, siteDescription, siteTitle, siteUrl } from '@/lib/shared';

export const metadata: Metadata = {
  title: { absolute: siteTitle },
  description: siteDescription,
  alternates: { canonical: '/' },
};

const features = [
  {
    icon: BatteryWarning,
    title: 'Alerte batterie faible',
    text: 'Sur batterie à 20 % ou moins, BatNotif te demande de brancher le chargeur, même si le chargeur branché est trop faible pour charger.',
  },
  {
    icon: BatteryCharging,
    title: 'Alerte à 80 % de charge',
    text: 'Branché à 80 % ou plus, il te rappelle de débrancher : la charge complète prolongée use la batterie plus vite.',
  },
  {
    icon: BellRing,
    title: 'Impossible à rater',
    text: 'Notification persistante et son en boucle tant que la situation n’est pas corrigée. Pas de toast fugace.',
  },
  {
    icon: Gamepad2,
    title: 'Même en plein écran',
    text: 'En jeu ou en vidéo plein écran, l’alerte s’agrandit et passe devant pour interrompre la session.',
  },
  {
    icon: Music,
    title: 'Ta musique',
    text: 'Un son MP3 ou WAV différent pour chaque alerte, ou les sons intégrés. Volume réglable et bouton « Tester ».',
  },
  {
    icon: PauseCircle,
    title: 'Snooze et pause',
    text: 'Snooze 15 min depuis la notification, pause 15 min, 1 h ou indéfinie. La pause survit au redémarrage.',
  },
  {
    icon: Keyboard,
    title: 'Discret et accessible',
    text: 'Une pastille colorée dans la zone de notification, un clic pour les réglages, Ctrl+Alt+B depuis n’importe où.',
  },
  {
    icon: ShieldCheck,
    title: 'Léger et respectueux',
    text: 'Gratuit, code source public, aucun droit administrateur, aucune donnée envoyée. Processeur au repos ≈ 0,004 %.',
  },
];

const steps = [
  { title: 'Télécharge', text: 'Un seul fichier .exe, rien à installer. Choisis la version standard ou compact.' },
  { title: 'Lance BatNotif', text: 'Une pastille verte affiche le niveau de batterie dans la zone de notification.' },
  { title: 'Oublie-le', text: 'Il te prévient au bon moment, avec ta musique, jusqu’à ce que tu branches ou débranches.' },
];

const jsonLd = {
  '@context': 'https://schema.org',
  '@type': 'SoftwareApplication',
  name: appName,
  description: siteDescription,
  url: siteUrl,
  applicationCategory: 'UtilitiesApplication',
  operatingSystem: 'Windows 10, Windows 11',
  softwareVersion: '0.1.0',
  inLanguage: 'fr',
  downloadUrl: releasesUrl,
  installUrl: releasesUrl,
  codeRepository: repoUrl,
  isAccessibleForFree: true,
  offers: { '@type': 'Offer', price: '0', priceCurrency: 'EUR' },
  author: { '@type': 'Person', name: 'Brandon Viry', url: 'https://github.com/brandonviry' },
  featureList: features.map((feature) => feature.title),
};

export default function HomePage() {
  return (
    <main className="flex flex-1 flex-col">
      <script
        type="application/ld+json"
        // Données structurées pour les moteurs de recherche (schema.org/SoftwareApplication).
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />

      {/* Héros */}
      <section className="relative overflow-hidden border-b">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0 bg-gradient-to-b from-[#2E9E5B]/15 via-transparent to-transparent"
        />
        <div className="relative mx-auto flex max-w-5xl flex-col items-center px-6 py-20 text-center md:py-28">
          <span className="mb-6 inline-flex items-center gap-2 rounded-full border bg-fd-card px-3 py-1 text-sm text-fd-muted-foreground">
            <span className="size-2 rounded-full bg-[#2E9E5B]" /> Gratuit · Windows 10 et 11 · v0.1.0
          </span>
          <h1 className="text-4xl font-bold tracking-tight text-balance md:text-6xl">
            Protège la santé de la batterie de ton PC portable
          </h1>
          <p className="mt-6 max-w-2xl text-lg text-fd-muted-foreground text-balance">
            BatNotif te prévient, avec la musique de ton choix, quand il faut <strong>brancher</strong> le
            chargeur (batterie faible) ou le <strong>débrancher</strong> (charge à 80 %). Une alerte qui
            insiste jusqu’à ce que tu agisses, pour éviter les deux extrêmes qui usent la batterie.
          </p>
          <div className="mt-10 flex flex-col gap-3 sm:flex-row">
            <a
              href={downloadUrls.standard}
              className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#2E9E5B] px-6 py-3 font-semibold text-white shadow-sm transition hover:bg-[#268a4f]"
            >
              <Download className="size-5" aria-hidden="true" />
              Télécharger pour Windows
            </a>
            <Link
              href="/docs"
              className="inline-flex items-center justify-center rounded-lg border bg-fd-card px-6 py-3 font-semibold transition hover:bg-fd-accent"
            >
              Lire la documentation
            </Link>
          </div>
          <p className="mt-4 text-sm text-fd-muted-foreground">
            Version standard, ≈ 160 Mo · aussi disponible en{' '}
            <a href={downloadUrls.compact} className="underline underline-offset-4">
              version compact (≈ 69 Mo)
            </a>{' '}
            · <Link href="/docs/installation" className="underline underline-offset-4">laquelle choisir ?</Link>
          </p>
        </div>
      </section>

      {/* Fonctionnalités */}
      <section className="mx-auto w-full max-w-6xl px-6 py-20" aria-labelledby="fonctionnalites">
        <h2 id="fonctionnalites" className="text-center text-3xl font-bold tracking-tight">
          Une alerte batterie qui ne se laisse pas ignorer
        </h2>
        <p className="mx-auto mt-4 max-w-2xl text-center text-fd-muted-foreground">
          La notification de Windows à 20 % passe inaperçue et ne dit rien quand la batterie reste à 100 % sur
          secteur. BatNotif comble ces deux trous.
        </p>
        <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {features.map(({ icon: Icon, title, text }) => (
            <article key={title} className="rounded-xl border bg-fd-card p-5">
              <Icon className="size-6 text-[#2E9E5B]" aria-hidden="true" />
              <h3 className="mt-4 font-semibold">{title}</h3>
              <p className="mt-2 text-sm text-fd-muted-foreground">{text}</p>
            </article>
          ))}
        </div>
      </section>

      {/* Santé de la batterie */}
      <section className="border-y bg-fd-card/50" aria-labelledby="pourquoi">
        <div className="mx-auto grid max-w-5xl gap-10 px-6 py-20 md:grid-cols-2 md:items-center">
          <div>
            <h2 id="pourquoi" className="text-3xl font-bold tracking-tight">
              Pourquoi garder sa batterie entre 20 et 80 % ?
            </h2>
            <p className="mt-4 text-fd-muted-foreground">
              Les batteries lithium-ion des PC portables vieillissent plus vite quand elles restent longtemps
              presque vides ou pleines et branchées. Rester dans la zone intermédiaire ralentit la perte de
              capacité, sans rien changer à ton usage.
            </p>
            <Link href="/docs/sante-batterie" className="mt-6 inline-block font-semibold text-[#2E9E5B] underline-offset-4 hover:underline">
              Comprendre la santé de la batterie →
            </Link>
          </div>
          <div className="rounded-xl border bg-fd-background p-6">
            <div className="flex h-6 overflow-hidden rounded-full border" role="img" aria-label="Zone de confort de la batterie entre 20 et 80 %">
              <div className="w-1/5 bg-[#E5484D]" />
              <div className="w-3/5 bg-[#2E9E5B]" />
              <div className="w-1/5 bg-[#F5A524]" />
            </div>
            <div className="mt-3 flex justify-between text-sm text-fd-muted-foreground">
              <span>0 %</span>
              <span>20 %</span>
              <span>80 %</span>
              <span>100 %</span>
            </div>
            <ul className="mt-6 space-y-2 text-sm">
              <li><span className="font-semibold text-[#E5484D]">≤ 20 % sur batterie</span> : BatNotif te demande de brancher.</li>
              <li><span className="font-semibold text-[#2E9E5B]">Entre les deux</span> : silence total.</li>
              <li><span className="font-semibold text-[#F5A524]">≥ 80 % sur secteur</span> : BatNotif te demande de débrancher.</li>
            </ul>
            <p className="mt-4 text-xs text-fd-muted-foreground">Seuils réglables de 5 à 95 %.</p>
          </div>
        </div>
      </section>

      {/* Étapes */}
      <section className="mx-auto w-full max-w-5xl px-6 py-20" aria-labelledby="demarrer">
        <h2 id="demarrer" className="text-center text-3xl font-bold tracking-tight">Prêt en trente secondes</h2>
        <ol className="mt-12 grid gap-6 md:grid-cols-3">
          {steps.map((step, index) => (
            <li key={step.title} className="rounded-xl border bg-fd-card p-6">
              <span className="inline-flex size-8 items-center justify-center rounded-full bg-[#2E9E5B] font-bold text-white">
                {index + 1}
              </span>
              <h3 className="mt-4 font-semibold">{step.title}</h3>
              <p className="mt-2 text-sm text-fd-muted-foreground">{step.text}</p>
            </li>
          ))}
        </ol>
      </section>

      {/* Versions */}
      <section className="border-t bg-fd-card/50" aria-labelledby="versions">
        <div className="mx-auto max-w-4xl px-6 py-20">
          <h2 id="versions" className="text-center text-3xl font-bold tracking-tight">Deux versions, même programme</h2>
          <div className="mt-10 grid gap-4 md:grid-cols-2">
            <div className="rounded-xl border-2 border-[#2E9E5B] bg-fd-background p-6">
              <p className="text-sm font-semibold text-[#2E9E5B]">Recommandée</p>
              <h3 className="mt-1 text-xl font-bold">Standard</h3>
              <p className="mt-3 text-sm text-fd-muted-foreground">≈ 160 Mo sur le disque · ≈ 133 Mo de mémoire. BatNotif tourne en permanence : c’est la plus économe en mémoire.</p>
              <a href={downloadUrls.standard} className="mt-6 inline-flex items-center gap-2 font-semibold text-[#2E9E5B] hover:underline">
                <Download className="size-4" aria-hidden="true" /> BatNotif-standard.exe
              </a>
            </div>
            <div className="rounded-xl border bg-fd-background p-6">
              <p className="text-sm font-semibold text-fd-muted-foreground">Plus petit fichier</p>
              <h3 className="mt-1 text-xl font-bold">Compact</h3>
              <p className="mt-3 text-sm text-fd-muted-foreground">≈ 69 Mo sur le disque · ≈ 194 Mo de mémoire. Pour ceux qui veulent le fichier le plus léger possible.</p>
              <a href={downloadUrls.compact} className="mt-6 inline-flex items-center gap-2 font-semibold hover:underline">
                <Download className="size-4" aria-hidden="true" /> BatNotif-compact.exe
              </a>
            </div>
          </div>
          <p className="mt-8 text-center text-sm text-fd-muted-foreground">
            Rien à installer, aucun droit administrateur, runtime .NET inclus. Code source sur{' '}
            <a href={repoUrl} className="underline underline-offset-4">GitHub</a>.
          </p>
        </div>
      </section>

      <footer className="border-t">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-3 px-6 py-8 text-sm text-fd-muted-foreground sm:flex-row">
          <p>© {new Date().getFullYear()} BatNotif · application gratuite pour Windows</p>
          <nav className="flex gap-6" aria-label="Pied de page">
            <Link href="/docs" className="hover:underline">Documentation</Link>
            <Link href="/docs/faq" className="hover:underline">FAQ</Link>
            <a href={repoUrl} className="hover:underline">GitHub</a>
          </nav>
        </div>
      </footer>
    </main>
  );
}
