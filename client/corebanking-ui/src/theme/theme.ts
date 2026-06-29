import { createTheme, alpha, lighten, darken, type Theme } from '@mui/material/styles';
import '@fontsource/inter/400.css';
import '@fontsource/inter/500.css';
import '@fontsource/inter/600.css';
import '@fontsource/inter/700.css';
import '@fontsource/plus-jakarta-sans/600.css';
import '@fontsource/plus-jakarta-sans/700.css';
import '@fontsource/plus-jakarta-sans/800.css';

export type ColorMode = 'light' | 'dark';

const DISPLAY = '"Plus Jakarta Sans", "Inter", "Helvetica", Arial, sans-serif';
const BODY = '"Inter", "Helvetica", Arial, sans-serif';

/**
 * Builds a complete light or dark MUI theme whose entire palette is derived
 * from the white-label brand colors. Surfaces, borders, shadows and component
 * overrides are tuned per mode so both read as a polished, intentional design.
 */
export function createAppTheme(mode: ColorMode, primary: string, secondary?: string): Theme {
  const isDark = mode === 'dark';

  // Derive a mode-appropriate accent: dark surfaces need a lighter, punchier
  // accent than the institutional brand hex that reads well on white.
  const primaryMain = isDark ? lighten(primary, 0.18) : primary;
  const secondaryMain = secondary ?? (isDark ? lighten(primary, 0.34) : lighten(primary, 0.12));

  // Layered surfaces.
  const bgDefault = isDark ? '#0A111F' : '#F5F7FB';
  const bgPaper = isDark ? '#111A2C' : '#FFFFFF';
  const surfaceRaised = isDark ? '#16213A' : '#FFFFFF';
  const divider = isDark ? alpha('#9DB2D6', 0.14) : '#E8ECF3';
  const cardBorder = isDark ? alpha('#9DB2D6', 0.12) : '#EAEEF4';

  const textPrimary = isDark ? '#E8EEF8' : '#0F1B2D';
  const textSecondary = isDark ? '#93A4BF' : '#5A6B83';
  const textDisabled = isDark ? alpha('#93A4BF', 0.5) : '#9AA7B8';

  // Soft tinted fill used behind icons / accents (works in both modes).
  const accentSoft = isDark ? alpha(primaryMain, 0.18) : alpha(primary, 0.09);

  const cardShadow = isDark
    ? '0 1px 2px rgba(0,0,0,0.4), 0 8px 24px -12px rgba(0,0,0,0.6)'
    : '0 1px 2px rgba(16,27,45,0.04), 0 6px 16px -10px rgba(16,27,45,0.12)';

  return createTheme({
    palette: {
      mode,
      primary: {
        main: primaryMain,
        light: lighten(primaryMain, 0.2),
        dark: darken(primaryMain, 0.2),
        contrastText: '#FFFFFF',
      },
      secondary: { main: secondaryMain, contrastText: '#FFFFFF' },
      background: { default: bgDefault, paper: bgPaper },
      text: { primary: textPrimary, secondary: textSecondary, disabled: textDisabled },
      divider,
      success: { main: isDark ? '#4ADE80' : '#16A34A' },
      error: { main: isDark ? '#F87171' : '#DC2626' },
      warning: { main: isDark ? '#FBBF24' : '#D97706' },
      info: { main: isDark ? '#60A5FA' : '#0284C7' },
    },
    typography: {
      fontFamily: BODY,
      h1: { fontFamily: DISPLAY, fontWeight: 800, letterSpacing: '-0.03em' },
      h2: { fontFamily: DISPLAY, fontWeight: 800, letterSpacing: '-0.03em' },
      h3: { fontFamily: DISPLAY, fontWeight: 700, letterSpacing: '-0.025em' },
      h4: { fontFamily: DISPLAY, fontWeight: 700, letterSpacing: '-0.02em' },
      h5: { fontFamily: DISPLAY, fontWeight: 700, letterSpacing: '-0.015em' },
      h6: { fontFamily: DISPLAY, fontWeight: 700, letterSpacing: '-0.01em' },
      subtitle1: { fontWeight: 600 },
      subtitle2: { fontWeight: 600, color: textSecondary },
      button: { fontWeight: 600 },
      body2: { color: textPrimary },
    },
    shape: { borderRadius: 12 },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            backgroundColor: bgDefault,
            // Subtle ambient gradient wash for depth (kept very faint).
            backgroundImage: isDark
              ? `radial-gradient(1200px 600px at 100% -10%, ${alpha(primaryMain, 0.12)} 0%, transparent 55%)`
              : `radial-gradient(1200px 600px at 100% -10%, ${alpha(primary, 0.06)} 0%, transparent 55%)`,
            backgroundAttachment: 'fixed',
          },
          '*::-webkit-scrollbar': { width: 10, height: 10 },
          '*::-webkit-scrollbar-thumb': {
            backgroundColor: isDark ? alpha('#9DB2D6', 0.22) : '#CBD5E1',
            borderRadius: 8,
            border: `2px solid ${bgDefault}`,
          },
          '*::-webkit-scrollbar-thumb:hover': {
            backgroundColor: isDark ? alpha('#9DB2D6', 0.34) : '#94A3B8',
          },
        },
      },
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: { textTransform: 'none', borderRadius: 10, fontWeight: 600, paddingInline: 16 },
          containedPrimary: {
            background: `linear-gradient(135deg, ${primaryMain} 0%, ${darken(primaryMain, 0.14)} 100%)`,
            boxShadow: `0 6px 16px -8px ${alpha(primaryMain, 0.7)}`,
            '&:hover': {
              background: `linear-gradient(135deg, ${lighten(primaryMain, 0.06)} 0%, ${darken(primaryMain, 0.08)} 100%)`,
              boxShadow: `0 8px 20px -8px ${alpha(primaryMain, 0.8)}`,
            },
          },
        },
      },
      MuiCard: {
        defaultProps: { elevation: 0 },
        styleOverrides: {
          root: {
            backgroundColor: surfaceRaised,
            backgroundImage: 'none',
            border: `1px solid ${cardBorder}`,
            borderRadius: 16,
            boxShadow: cardShadow,
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: { backgroundImage: 'none' },
          rounded: { borderRadius: 16 },
          outlined: { borderColor: cardBorder },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: { fontWeight: 600, borderRadius: 8 },
        },
      },
      MuiTableHead: {
        styleOverrides: {
          root: {
            '& .MuiTableCell-head': {
              backgroundColor: isDark ? alpha('#9DB2D6', 0.05) : '#F8FAFC',
              fontWeight: 700,
              fontSize: '0.7rem',
              textTransform: 'uppercase',
              letterSpacing: '0.06em',
              color: textSecondary,
              borderBottom: `1px solid ${divider}`,
            },
          },
        },
      },
      MuiTableCell: {
        styleOverrides: { root: { borderColor: divider } },
      },
      MuiTableRow: {
        styleOverrides: {
          root: { '&:hover': { backgroundColor: accentSoft } },
        },
      },
      MuiDrawer: {
        styleOverrides: {
          paper: { backgroundImage: 'none', border: 'none' },
        },
      },
      MuiAppBar: {
        defaultProps: { elevation: 0, color: 'default' },
        styleOverrides: {
          root: {
            borderBottom: `1px solid ${divider}`,
            backgroundColor: alpha(bgPaper, isDark ? 0.7 : 0.8),
            backdropFilter: 'blur(12px)',
            backgroundImage: 'none',
          },
        },
      },
      MuiTextField: { defaultProps: { variant: 'outlined', size: 'small' } },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            borderRadius: 10,
            backgroundColor: isDark ? alpha('#9DB2D6', 0.05) : '#FFFFFF',
          },
        },
      },
      MuiAlert: { styleOverrides: { root: { borderRadius: 12 } } },
      MuiTooltip: {
        styleOverrides: {
          tooltip: { borderRadius: 8, fontSize: '0.75rem', fontWeight: 500 },
        },
      },
      MuiListItemButton: {
        styleOverrides: {
          root: {
            borderRadius: 10,
            '&.Mui-selected': {
              backgroundColor: accentSoft,
              color: primaryMain,
              '& .MuiListItemIcon-root': { color: primaryMain },
              '&:hover': { backgroundColor: accentSoft },
            },
          },
        },
      },
    },
  });
}

/** Reusable sx fragment for monospaced/tabular money figures. */
export const tabularNums = {
  fontVariantNumeric: 'tabular-nums',
  fontFeatureSettings: '"tnum"',
} as const;
