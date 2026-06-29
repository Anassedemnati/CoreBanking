import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { loadBranding } from './loadBranding';
import { type BrandConfig } from './types';

const BrandContext = createContext<BrandConfig | null>(null);

/**
 * Loads the white-label config before first paint. While loading it shows a
 * minimal neutral splash (the brand color isn't known yet); once resolved it
 * sets the document title and renders the app with the brand in context.
 */
export function BrandProvider({ children }: { children: ReactNode }) {
  const [brand, setBrand] = useState<BrandConfig | null>(null);

  useEffect(() => {
    let active = true;
    loadBranding().then((b) => {
      if (!active) return;
      document.title = `${b.bankName} · ${b.tagline}`;
      setBrand(b);
    });
    return () => {
      active = false;
    };
  }, []);

  if (!brand) return <BrandSplash />;

  return <BrandContext.Provider value={brand}>{children}</BrandContext.Provider>;
}

export function useBrand(): BrandConfig {
  const ctx = useContext(BrandContext);
  if (!ctx) throw new Error('useBrand must be used inside BrandProvider');
  return ctx;
}

function BrandSplash() {
  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: '#0b1220',
      }}
    >
      <div
        style={{
          width: 36,
          height: 36,
          borderRadius: '50%',
          border: '3px solid rgba(255,255,255,0.15)',
          borderTopColor: 'rgba(255,255,255,0.85)',
          animation: 'cb-spin 0.8s linear infinite',
        }}
      />
      <style>{`@keyframes cb-spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  );
}
