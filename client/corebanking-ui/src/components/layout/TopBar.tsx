import {
  AppBar,
  Toolbar,
  IconButton,
  Typography,
  Box,
  Avatar,
  Tooltip,
  Menu,
  MenuItem,
  Divider,
} from '@mui/material';
import MenuRoundedIcon from '@mui/icons-material/MenuRounded';
import LogoutRoundedIcon from '@mui/icons-material/LogoutRounded';
import { useState } from 'react';
import { useAuth, ROLE_LABELS } from '../../auth/AuthProvider';
import { ColorModeToggle } from '../common/ColorModeToggle';
import { DRAWER_WIDTH } from './Sidebar';

interface Props {
  onMenuToggle: () => void;
}

export function TopBar({ onMenuToggle }: Props) {
  const { user, logout } = useAuth();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  return (
    <AppBar
      position="fixed"
      sx={{
        width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
        ml: { md: `${DRAWER_WIDTH}px` },
        zIndex: (t) => t.zIndex.drawer - 1,
      }}
    >
      <Toolbar sx={{ gap: 1, minHeight: { xs: 64, md: 72 } }}>
        <IconButton edge="start" onClick={onMenuToggle} sx={{ display: { md: 'none' } }}>
          <MenuRoundedIcon />
        </IconButton>

        <Box flexGrow={1} />

        <ColorModeToggle />

        <Tooltip title="Account">
          <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} sx={{ ml: 0.5 }}>
            <Avatar
              sx={{
                width: 36,
                height: 36,
                fontSize: 15,
                fontWeight: 700,
                background: (t) =>
                  `linear-gradient(135deg, ${t.palette.primary.light}, ${t.palette.primary.dark})`,
              }}
            >
              {user?.fullName?.charAt(0).toUpperCase() ?? '?'}
            </Avatar>
          </IconButton>
        </Tooltip>

        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={() => setAnchorEl(null)}
          transformOrigin={{ horizontal: 'right', vertical: 'top' }}
          anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
          slotProps={{ paper: { sx: { mt: 1, minWidth: 220, borderRadius: 2 } } }}
        >
          <Box px={2} py={1.5}>
            <Typography variant="subtitle2" noWrap>
              {user?.fullName}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {user?.primaryRole ? ROLE_LABELS[user.primaryRole] : ''}
            </Typography>
          </Box>
          <Divider />
          <MenuItem
            onClick={() => {
              setAnchorEl(null);
              logout();
            }}
            sx={{ color: 'error.main', mt: 0.5, gap: 1.5 }}
          >
            <LogoutRoundedIcon fontSize="small" />
            Sign Out
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
}
