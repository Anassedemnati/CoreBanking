/**
 * White-label branding configuration.
 *
 * Loaded at runtime from `public/branding.json` so a single build can be
 * re-skinned per bank — drop in a `branding.json` and logo assets, no rebuild.
 * The full MUI theme (light + dark) is derived from `primaryColor`.
 */
export interface BrandConfig {
  /** Full bank name, shown in the sidebar lockup and document title. */
  bankName: string;
  /** Short tagline shown under the bank name (optional). */
  tagline: string;
  /** 2–3 letter monogram used when no logo image is configured. */
  shortName: string;
  /** Brand accent — the entire palette is derived from this hex color. */
  primaryColor: string;
  /** Optional secondary accent; defaults to a tint of primary when omitted. */
  secondaryColor?: string;
  /** Full logo lockup (wide). Falls back to the monogram + name when absent. */
  logoUrl?: string;
  /** Square/icon logo for collapsed and mobile contexts. */
  logoIconUrl?: string;
}

/**
 * Built-in defaults. Used when `branding.json` is missing or malformed so the
 * app always renders rather than hard-failing on a bad config.
 */
export const DEFAULT_BRAND: BrandConfig = {
  bankName: 'CoreBanking',
  tagline: 'Staff Portal',
  shortName: 'CB',
  primaryColor: '#0D47A1',
  secondaryColor: '#1565C0',
  logoUrl: undefined,
  logoIconUrl: undefined,
};
