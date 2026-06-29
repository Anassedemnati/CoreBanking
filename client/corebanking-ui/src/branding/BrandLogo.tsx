import { Box, Typography } from '@mui/material';
import { useBrand } from './BrandProvider';

interface Props {
  /** 'full' = monogram/logo + name lockup; 'icon' = square mark only. */
  variant?: 'full' | 'icon';
  /** Render name + tagline in white (for dark sidebar headers). */
  onDark?: boolean;
  size?: number;
}

/**
 * Renders the configured brand. Prefers the logo image asset when present,
 * otherwise falls back to a tinted monogram tile + bank name.
 */
export function BrandLogo({ variant = 'full', onDark = false, size = 36 }: Props) {
  const brand = useBrand();

  const monogram = (
    <Box
      sx={{
        width: size,
        height: size,
        flexShrink: 0,
        borderRadius: size / 3.2,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        color: 'common.white',
        fontWeight: 800,
        fontSize: size * 0.4,
        letterSpacing: '-0.02em',
        background: (t) =>
          `linear-gradient(135deg, ${t.palette.primary.light} 0%, ${t.palette.primary.dark} 100%)`,
        boxShadow: (t) => `0 4px 12px ${t.palette.primary.main}40`,
      }}
    >
      {brand.shortName}
    </Box>
  );

  if (variant === 'icon') {
    const iconSrc = brand.logoIconUrl ?? brand.logoUrl;
    return iconSrc ? (
      <Box
        component="img"
        src={iconSrc}
        alt={brand.bankName}
        sx={{ width: size, height: size, objectFit: 'contain', flexShrink: 0 }}
      />
    ) : (
      monogram
    );
  }

  // Full lockup: a wide logo image replaces the whole lockup when provided.
  if (brand.logoUrl) {
    return (
      <Box
        component="img"
        src={brand.logoUrl}
        alt={brand.bankName}
        sx={{ height: size, maxWidth: '100%', objectFit: 'contain' }}
      />
    );
  }

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, minWidth: 0 }}>
      {monogram}
      <Box sx={{ minWidth: 0 }}>
        <Typography
          noWrap
          sx={{
            fontFamily: '"Plus Jakarta Sans", "Inter", sans-serif',
            fontWeight: 800,
            fontSize: size * 0.42,
            lineHeight: 1.1,
            letterSpacing: '-0.02em',
            color: onDark ? 'common.white' : 'text.primary',
          }}
        >
          {brand.bankName}
        </Typography>
        <Typography
          noWrap
          sx={{
            fontSize: size * 0.26,
            fontWeight: 500,
            letterSpacing: '0.04em',
            textTransform: 'uppercase',
            color: onDark ? 'rgba(255,255,255,0.55)' : 'text.secondary',
          }}
        >
          {brand.tagline}
        </Typography>
      </Box>
    </Box>
  );
}
