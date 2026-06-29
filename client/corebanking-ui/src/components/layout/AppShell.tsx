import { useState } from 'react';
import { Box, useMediaQuery, useTheme } from '@mui/material';
import { Outlet } from 'react-router-dom';
import { TopBar } from './TopBar';
import { Sidebar, DRAWER_WIDTH } from './Sidebar';

export function AppShell() {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Sidebar
        mobileOpen={mobileOpen}
        onMobileClose={() => setMobileOpen(false)}
        isMobile={isMobile}
      />
      <Box sx={{ flexGrow: 1, width: { md: `calc(100% - ${DRAWER_WIDTH}px)` } }}>
        <TopBar onMenuToggle={() => setMobileOpen((v) => !v)} />
        <Box
          component="main"
          sx={{
            p: { xs: 2, sm: 3, md: 4 },
            mt: { xs: '64px', md: '72px' },
            maxWidth: 1400,
            mx: 'auto',
          }}
        >
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
}
