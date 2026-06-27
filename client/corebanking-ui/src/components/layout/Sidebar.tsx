import {
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Divider,
  Box,
  Typography,
  Collapse,
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import PeopleIcon from '@mui/icons-material/People';
import PersonAddIcon from '@mui/icons-material/PersonAdd';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import AddCardIcon from '@mui/icons-material/AddCard';
import CategoryIcon from '@mui/icons-material/Category';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import { useLocation, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { useAuth } from '../../auth/AuthProvider';
import { CAN, type Role } from '../../auth/roles';

export const DRAWER_WIDTH = 248;

interface NavItem {
  label: string;
  icon: React.ReactNode;
  path?: string;
  children?: NavItem[];
  roles?: readonly Role[];
}

const NAV_ITEMS: NavItem[] = [
  {
    label: 'Dashboard',
    icon: <DashboardIcon fontSize="small" />,
    path: '/',
  },
  {
    label: 'Clients',
    icon: <PeopleIcon fontSize="small" />,
    children: [
      { label: 'All Clients', icon: <PeopleIcon fontSize="small" />, path: '/clients' },
      {
        label: 'Register Client',
        icon: <PersonAddIcon fontSize="small" />,
        path: '/clients/new',
        roles: CAN.registerClient,
      },
    ],
  },
  {
    label: 'Products',
    icon: <CategoryIcon fontSize="small" />,
    children: [
      { label: 'All Products', icon: <CategoryIcon fontSize="small" />, path: '/products' },
      {
        label: 'Create Product',
        icon: <AddCircleOutlineIcon fontSize="small" />,
        path: '/products/new',
        roles: CAN.createProduct,
      },
    ],
  },
  {
    label: 'Accounts',
    icon: <AccountBalanceIcon fontSize="small" />,
    children: [
      { label: 'All Accounts', icon: <AccountBalanceIcon fontSize="small" />, path: '/accounts' },
      {
        label: 'Open Account',
        icon: <AddCardIcon fontSize="small" />,
        path: '/accounts/new',
        roles: CAN.submitAccount,
      },
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
  const [openSections, setOpenSections] = useState<Record<string, boolean>>({
    Clients: true,
    Products: true,
    Accounts: true,
  });

  const toggleSection = (label: string) =>
    setOpenSections((prev) => ({ ...prev, [label]: !prev[label] }));

  const content = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {/* Brand header */}
      <Box
        sx={{
          px: 2.5,
          py: 2,
          display: { md: 'none' },
          borderBottom: '1px solid',
          borderColor: 'divider',
        }}
      >
        <Typography variant="h6" color="primary.main" fontWeight={700}>
          CoreBanking
        </Typography>
      </Box>

      <Box sx={{ overflow: 'auto', flexGrow: 1, py: 1.5, px: 1.5 }}>
        <List disablePadding>
          {NAV_ITEMS.map((item) => {
            if (item.children) {
              const visibleChildren = item.children.filter(
                (c) => !c.roles || hasRole(...(c.roles as Role[])),
              );
              if (visibleChildren.length === 0) return null;

              const isOpen = openSections[item.label] ?? true;
              return (
                <Box key={item.label}>
                  <ListItemButton
                    onClick={() => toggleSection(item.label)}
                    sx={{ borderRadius: 1, mb: 0.25 }}
                  >
                    <ListItemIcon sx={{ minWidth: 36 }}>{item.icon}</ListItemIcon>
                    <ListItemText
                      primary={item.label}
                      primaryTypographyProps={{ fontWeight: 600, fontSize: '0.875rem' }}
                    />
                    {isOpen ? <ExpandLessIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
                  </ListItemButton>
                  <Collapse in={isOpen}>
                    <List disablePadding sx={{ pl: 1 }}>
                      {visibleChildren.map((child) => (
                        <ListItemButton
                          key={child.path}
                          selected={location.pathname === child.path}
                          onClick={() => {
                            navigate(child.path!);
                            if (isMobile) onMobileClose();
                          }}
                          sx={{ borderRadius: 1, mb: 0.25, py: 0.75 }}
                        >
                          <ListItemIcon sx={{ minWidth: 32, color: 'text.secondary' }}>
                            {child.icon}
                          </ListItemIcon>
                          <ListItemText
                            primary={child.label}
                            primaryTypographyProps={{ fontSize: '0.85rem' }}
                          />
                        </ListItemButton>
                      ))}
                    </List>
                  </Collapse>
                  <Divider sx={{ my: 0.5 }} />
                </Box>
              );
            }

            return (
              <ListItemButton
                key={item.path}
                selected={location.pathname === item.path}
                onClick={() => {
                  navigate(item.path!);
                  if (isMobile) onMobileClose();
                }}
                sx={{ borderRadius: 1, mb: 0.25 }}
              >
                <ListItemIcon sx={{ minWidth: 36 }}>{item.icon}</ListItemIcon>
                <ListItemText
                  primary={item.label}
                  primaryTypographyProps={{ fontWeight: 500, fontSize: '0.875rem' }}
                />
              </ListItemButton>
            );
          })}
        </List>
      </Box>

      {/* Version footer */}
      <Box sx={{ p: 2, borderTop: '1px solid', borderColor: 'divider' }}>
        <Typography variant="caption" color="text.disabled">
          CoreBanking Staff Portal v1.0
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
        sx={{ '& .MuiDrawer-paper': { width: DRAWER_WIDTH } }}
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
        '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box', top: 64 },
      }}
    >
      {content}
    </Drawer>
  );
}
