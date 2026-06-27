import { createTheme } from '@mui/material/styles';
import '@fontsource/inter/400.css';
import '@fontsource/inter/500.css';
import '@fontsource/inter/600.css';
import '@fontsource/inter/700.css';

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#0D47A1',
      light: '#1976D2',
      dark: '#0A2D6B',
      contrastText: '#ffffff',
    },
    secondary: {
      main: '#1565C0',
      contrastText: '#ffffff',
    },
    background: {
      default: '#F0F4F8',
      paper: '#ffffff',
    },
    success: { main: '#2E7D32', light: '#E8F5E9' },
    error: { main: '#C62828', light: '#FFEBEE' },
    warning: { main: '#EF6C00', light: '#FFF3E0' },
    info: { main: '#0277BD', light: '#E1F5FE' },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h4: { fontWeight: 700, letterSpacing: '-0.5px' },
    h5: { fontWeight: 600, letterSpacing: '-0.25px' },
    h6: { fontWeight: 600 },
    subtitle1: { fontWeight: 600 },
    subtitle2: { fontWeight: 600, color: '#6B7280' },
    body2: { color: '#374151' },
  },
  shape: { borderRadius: 8 },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 600,
          borderRadius: 6,
          boxShadow: 'none',
          '&:hover': { boxShadow: 'none' },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          boxShadow: '0 1px 3px rgba(0,0,0,0.07), 0 1px 2px rgba(0,0,0,0.05)',
          borderRadius: 12,
          border: '1px solid rgba(0,0,0,0.06)',
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: { fontWeight: 500, borderRadius: 6 },
      },
    },
    MuiTableHead: {
      styleOverrides: {
        root: {
          '& .MuiTableCell-head': {
            backgroundColor: '#F8FAFC',
            fontWeight: 600,
            fontSize: '0.75rem',
            textTransform: 'uppercase',
            letterSpacing: '0.05em',
            color: '#6B7280',
            borderBottom: '2px solid #E5E7EB',
          },
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: { borderColor: '#F3F4F6' },
      },
    },
    MuiDrawer: {
      styleOverrides: {
        paper: {
          borderRight: '1px solid #E5E7EB',
          backgroundColor: '#ffffff',
        },
      },
    },
    MuiAppBar: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: {
          borderBottom: '1px solid #E5E7EB',
          backgroundColor: '#ffffff',
        },
      },
    },
    MuiTextField: {
      defaultProps: { variant: 'outlined', size: 'small' },
    },
    MuiAlert: {
      styleOverrides: {
        root: { borderRadius: 8 },
      },
    },
    MuiPaper: {
      styleOverrides: {
        rounded: { borderRadius: 12 },
      },
    },
    MuiListItemButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          mx: 1,
          '&.Mui-selected': {
            backgroundColor: '#EFF6FF',
            color: '#0D47A1',
            '& .MuiListItemIcon-root': { color: '#0D47A1' },
            '&:hover': { backgroundColor: '#DBEAFE' },
          },
        },
      },
    },
  },
});
