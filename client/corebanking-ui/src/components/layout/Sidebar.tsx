import {
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Box,
  Typography,
} from '@mui/material';
import DashboardRoundedIcon from '@mui/icons-material/DashboardRounded';
import PeopleRoundedIcon from '@mui/icons-material/PeopleRounded';
import PersonAddRoundedIcon from '@mui/icons-material/PersonAddRounded';
import AccountBalanceRoundedIcon from '@mui/icons-material/AccountBalanceRounded';
import AddCardRoundedIcon from '@mui/icons-material/AddCardRounded';
import CategoryRoundedIcon from '@mui/icons-material/CategoryRounded';
import AddCircleOutlineRoundedIcon from '@mui/icons-material/AddCircleOutlineRounded';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/AuthProvider';
import { CAN, type Role } from '../../auth/roles';
import { BrandLogo } from '../../branding/BrandLogo';

export const DRAWER_WIDTH = 264;

interface NavItem {
  label: string;
  icon: React.ReactNode;
  path: string;
  roles?: readonly Role[];
}

interface NavSection {
  heading: string;
  items: NavItem[];
}

const NAV_SECTIONS: NavSection[] = [
  {
    heading: 'Overview',
    items: [{ label: 'Dashboard', icon: <DashboardRoundedIcon fontSize="small" />, path: '/' }],
  },
  {
    heading: 'Clients',
    items: [
      { label: 'All Clients', icon: <PeopleRoundedIcon fontSize="small" />, path: '/clients' },
      { label: 'Register Client', icon: <PersonAddRoundedIcon fontSize="small" />, path: '/clients/new', roles: CAN.registerClient },
    ],
  },
  {
    heading: 'Products',
    items: [
      { label: 'All Products', icon: <CategoryRoundedIcon fontSize="small" />, path: '/products' },
      { label: 'Create Product', icon: <AddCircleOutlineRoundedIcon fontSize="small" />, path: '/products/new', roles: CAN.createProduct },
    ],
  },
  {
    heading: 'Accounts',
    items: [
      { label: 'All Accounts', icon: <AccountBalanceRoundedIcon fontSize="small" />, path: '/accounts' },
      { label: 'Open Account', icon: <AddCardRoundedIcon fontSize="small" />, path: '/accounts/new', roles: CAN.submitAccount },
    ],
  },
];

interface SidebarProps {
  mobileOpen: boolean;
  onMobileClose: () => void;
  isMobile: boolean;
}

export function Sidebar({ mobileOpen, onMobileClose, isMobile }: SidebarProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const { hasRole } = useAuth();

  const isActive = (path: string) =>
    path === '/' ? location.pathname === '/' : location.pathname === path;

  const go = (path: string) => {
    navigate(path);
    if (isMobile) onMobileClose();
  };

  const content = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {/* Brand header */}
      <Box
        sx={{
          height: 72,
          px: 2.5,
          display: 'flex',
          alignItems: 'center',
          borderBottom: '1px solid',
          borderColor: 'divider',
        }}
      >
        <BrandLogo variant="full" size={34} />
      </Box>

      <Box sx={{ overflowY: 'auto', flexGrow: 1, py: 2, px: 1.75 }}>
        {NAV_SECTIONS.map((section) => {
          const visible = section.items.filter((i) => !i.roles || hasRole(...(i.roles as Role[])));
          if (visible.length === 0) return null;
          return (
            <Box key={section.heading} sx={{ mb: 2.5 }}>
              <Typography
                sx={{
                  px: 1.5,
                  mb: 0.75,
                  fontSize: '0.66rem',
                  fontWeight: 700,
                  letterSpacing: '0.09em',
                  textTransform: 'uppercase',
                  color: 'text.disabled',
                }}
              >
                {section.heading}
              </Typography>
              <List disablePadding sx={{ display: 'flex', flexDirection: 'column', gap: 0.25 }}>
                {visible.map((item) => {
                  const active = isActive(item.path);
                  return (
                    <ListItemButton
                      key={item.path}
                      selected={active}
                      onClick={() => go(item.path)}
                      sx={{
                        py: 0.9,
                        position: 'relative',
                        '&.Mui-selected::before': {
                          content: '""',
                          position: 'absolute',
                          left: 0,
                          top: '50%',
                          transform: 'translateY(-50%)',
                          width: 3,
                          height: 20,
                          borderRadius: 4,
                          backgroundColor: 'primary.main',
                        },
                      }}
                    >
                      <ListItemIcon sx={{ minWidth: 34, color: active ? 'primary.main' : 'text.secondary' }}>
                        {item.icon}
                      </ListItemIcon>
                      <ListItemText
                        primary={item.label}
                        primaryTypographyProps={{
                          fontSize: '0.875rem',
                          fontWeight: active ? 700 : 500,
                        }}
                      />
                    </ListItemButton>
                  );
                })}
              </List>
            </Box>
          );
        })}
      </Box>

      {/* Footer */}
      <Box sx={{ p: 2, borderTop: '1px solid', borderColor: 'divider' }}>
        <Typography variant="caption" color="text.disabled">
          Staff Portal · v1.0
        </Typography>
      </Box>
    </Box>
  );

  if (isMobile) {
    return (
      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={onMobileClose}
        ModalProps={{ keepMounted: true }}
        sx={{
          '& .MuiDrawer-paper': {
            width: DRAWER_WIDTH,
            backgroundColor: 'background.paper',
          },
        }}
      >
        {content}
      </Drawer>
    );
  }

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: DRAWER_WIDTH,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: DRAWER_WIDTH,
          boxSizing: 'border-box',
          backgroundColor: 'background.paper',
          borderRight: '1px solid',
          borderColor: 'divider',
        },
      }}
    >
      {content}
    </Drawer>
  );
}
