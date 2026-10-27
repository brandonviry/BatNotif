import { ImageResponse } from 'next/og';

export const alt = 'BatNotif — alerte batterie pour préserver la santé de la batterie de ton PC portable';
export const size = { width: 1200, height: 630 };
export const contentType = 'image/png';

export default function OpenGraphImage() {
  return new ImageResponse(
    (
      <div
        style={{
          width: '100%',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'center',
          padding: '72px',
          background: 'linear-gradient(135deg, #0f1f17 0%, #14532d 55%, #2E9E5B 100%)',
          color: '#ffffff',
          fontFamily: 'sans-serif',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '24px' }}>
          <div
            style={{
              width: '96px',
              height: '96px',
              borderRadius: '48px',
              background: '#2E9E5B',
              border: '4px solid rgba(255,255,255,0.35)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '44px',
              fontWeight: 700,
            }}
          >
            80
          </div>
          <div style={{ fontSize: '64px', fontWeight: 700 }}>BatNotif</div>
        </div>
        <div style={{ marginTop: '40px', fontSize: '48px', fontWeight: 700, lineHeight: 1.2 }}>
          Protège la santé de la batterie de ton PC portable
        </div>
        <div style={{ marginTop: '24px', fontSize: '30px', color: 'rgba(255,255,255,0.85)' }}>
          Alerte à 20 % et à 80 % · son en boucle · gratuit pour Windows
        </div>
      </div>
    ),
    size,
  );
}
