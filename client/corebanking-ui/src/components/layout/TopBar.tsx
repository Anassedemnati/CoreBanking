import { AppBar, Toolbar, IconButton, Typography, Box, Avatar, Tooltip, Menu, MenuItem } from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import AccountCircleIcon from '@mui/icons-material/AccountCircle';
import { useState } from 'react';
import { useAuth, ROLE_LABELS } from '../../auth/AuthProvider';

interface Props {
  onMenuToggle: () => void;
}

export function TopBar({ onMenuToggle }: Props) {
  const { user, logout } = useAuth();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  return (
    <AppBar
      position="fixed"
      color="default"
      sx={{ zIndex: (t) => t.zIndex.drawer + 1 }}
    >
      <Toolbar sx={{ gap: 2 }}>
        <IconButton
          edge="start"
          onClick={onMenuToggle}
          sx={{ display: { md: 'none' } }}
        >
          <MenuIcon />
        </IconButton>

        {/* Logo / Brand */}
        <Box display="flex" alignItems="center" gap={1}>
          <Box
            sx={{
              width: 32,
              height: 32,
              bgcolor: 'primary.main',
              borderRadius: 1,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <Typography sx={{ color: 'white', fontWeight: 800, fontSize: 14 }}>CB</Typography>
          </Box>
          <Typography variant="h6" color="primary.main" sx={{ fontWeight: 700 }}>
            CoreBanking
          </Typography>
        </Box>

        <Box flexGrow={1} />

        {/* User menu */}
        <Tooltip title="Account">
          <IconButton onClick={(e) => setAnchorEl(e.currentTarget)}>
            <Avatar sx={{ width: 34, height: 34, bgcolor: 'primary.main', fontSize: 14 }}>
              {user?.fullName?.charAt(0).toUpperCase() ?? <AccountCircleIcon />}
            </Avatar>
          </IconButton>
        </Tooltip>

        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={() => setAnchorEl(null)}
          transformOrigin={{ horizontal: 'right', vertical: 'top' }}
          anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
        >
          <Box px={2} py={1.5}>
            <Typography variant="subtitle2">{user?.fullName}</Typography>
            <Typography variant="caption" color="text.secondary">
              {user?.primaryRole ? ROLE_LABELS[user.primaryRole] : ''}
            </Typography>
          </Box>
          <MenuItem
            onClick={() => {
              setAnchorEl(null);
              logout();
            }}
            sx={{ color: 'error.main' }}
          >
            Sign Out
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
}
