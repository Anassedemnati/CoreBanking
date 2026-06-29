import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { useBrand } from '../branding/BrandProvider';
import { createAppTheme, type ColorMode } from './theme';

interface ColorModeContextValue {
  mode: ColorMode;
  toggle: () => void;
  setMode: (mode: ColorMode) => void;
}

const ColorModeContext = createContext<ColorModeContextValue | null>(null);
const STORAGE_KEY = 'cb.colorMode';

function initialMode(): ColorMode {
  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored === 'light' || stored === 'dark') return stored;
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

/**
 * Owns the active color mode (persisted, OS-aware on first visit) and builds
 * the brand-derived theme for that mode. Must live inside BrandProvider.
 */
export function ColorModeProvider({ children }: { children: ReactNode }) {
  const brand = useBrand();
  const [mode, setModeState] = useState<ColorMode>(initialMode);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, mode);
    document.documentElement.style.colorScheme = mode;
  }, [mode]);

  const theme = useMemo(
    () => createAppTheme(mode, brand.primaryColor, brand.secondaryColor),
    [mode, brand.primaryColor, brand.secondaryColor],
  );

  const ctx = useMemo<ColorModeContextValue>(
    () => ({
      mode,
      toggle: () => setModeState((m) => (m === 'light' ? 'dark' : 'light')),
      setMode: setModeState,
    }),
    [mode],
  );

  return (
    <ColorModeContext.Provider value={ctx}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </ThemeProvider>
    </ColorModeContext.Provider>
  );
}

export function useColorMode(): ColorModeContextValue {
  const ctx = useContext(ColorModeContext);
  if (!ctx) throw new Error('useColorMode must be used inside ColorModeProvider');
  return ctx;
}
