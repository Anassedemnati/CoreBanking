import { DEFAULT_BRAND, type BrandConfig } from './types';

const HEX = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i;

/**
 * Fetch `public/branding.json` and merge it over the built-in defaults.
 *
 * Resilient by design: a missing file, non-200 response, invalid JSON, or a
 * bad `primaryColor` all fall back to defaults so the portal still boots.
 */
export async function loadBranding(): Promise<BrandConfig> {
  try {
    const res = await fetch('/branding.json', { cache: 'no-store' });
    if (!res.ok) return DEFAULT_BRAND;

    const raw = (await res.json()) as Partial<BrandConfig>;
    const primaryColor =
      typeof raw.primaryColor === 'string' && HEX.test(raw.primaryColor)
        ? raw.primaryColor
        : DEFAULT_BRAND.primaryColor;
    const secondaryColor =
      typeof raw.secondaryColor === 'string' && HEX.test(raw.secondaryColor)
        ? raw.secondaryColor
        : undefined;

    return {
      bankName: raw.bankName?.trim() || DEFAULT_BRAND.bankName,
      tagline: raw.tagline?.trim() || DEFAULT_BRAND.tagline,
      shortName: (raw.shortName?.trim() || DEFAULT_BRAND.shortName).slice(0, 3),
      primaryColor,
      secondaryColor,
      logoUrl: raw.logoUrl?.trim() || undefined,
      logoIconUrl: raw.logoIconUrl?.trim() || undefined,
    };
  } catch {
    return DEFAULT_BRAND;
  }
}
