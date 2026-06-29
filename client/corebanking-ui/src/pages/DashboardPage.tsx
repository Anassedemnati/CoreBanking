import { useMemo } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Divider,
  Stack,
  Button,
  useTheme,
} from '@mui/material';
import PeopleRoundedIcon from '@mui/icons-material/PeopleRounded';
import AccountBalanceWalletRoundedIcon from '@mui/icons-material/AccountBalanceWalletRounded';
import CategoryRoundedIcon from '@mui/icons-material/CategoryRounded';
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import PersonAddRoundedIcon from '@mui/icons-material/PersonAddRounded';
import AddCardRoundedIcon from '@mui/icons-material/AddCardRounded';
import AddCircleOutlineRoundedIcon from '@mui/icons-material/AddCircleOutlineRounded';
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded';
import { format, parseISO } from 'date-fns';
import { useNavigate } from 'react-router-dom';
import { useClients } from '../api/clients.api';
import { useProducts } from '../api/products.api';
import { useAccounts } from '../api/accounts.api';
import { useAuth, ROLE_LABELS } from '../auth/AuthProvider';
import { CAN, type Role } from '../auth/roles';
import { StatCard } from '../components/dashboard/StatCard';
import { AccountsTrendChart, type TrendPoint } from '../components/dashboard/AccountsTrendChart';
import { StatusDonut, type DonutSlice } from '../components/dashboard/StatusDonut';
import { StatusChip } from '../components/common/StatusChip';
import { tabularNums } from '../theme/theme';
import type { SavingsAccountDto } from '../api/types';

function greeting(): string {
  const h = new Date().getHours();
  if (h < 12) return 'Good morning';
  if (h < 18) return 'Good afternoon';
  return 'Good evening';
}

function buildTrend(accounts: SavingsAccountDto[]): TrendPoint[] {
  const byMonth = new Map<string, number>();
  for (const a of accounts) {
    if (!a.submittedOn) continue;
    try {
      const key = format(parseISO(a.submittedOn), 'yyyy-MM');
      byMonth.set(key, (byMonth.get(key) ?? 0) + 1);
    } catch {
      /* skip unparseable dates */
    }
  }
  return [...byMonth.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .slice(-6)
    .map(([key, count]) => ({ label: format(parseISO(`${key}-01`), 'MMM'), count }));
}

export default function DashboardPage() {
  const theme = useTheme();
  const navigate = useNavigate();
  const { user, hasRole } = useAuth();
  const { data: clients, isLoading: loadingClients } = useClients();
  const { data: products, isLoading: loadingProducts } = useProducts();
  const { data: accounts, isLoading: loadingAccounts } = useAccounts();

  const accs = useMemo(() => accounts ?? [], [accounts]);
  const activeAccounts = accs.filter((a) => a.status === 'Active').length;
  const totalBalance = accs.reduce((sum, a) => sum + (a.accountBalance ?? 0), 0);

  const trend = useMemo(() => buildTrend(accs), [accs]);

  const donut = useMemo<DonutSlice[]>(() => {
    const palette: Record<string, string> = {
      Active: theme.palette.success.main,
      Submitted: theme.palette.info.main,
      Approved: theme.palette.warning.main,
      Closed: theme.palette.text.disabled,
      Withdrawn: theme.palette.text.disabled,
      Rejected: theme.palette.error.main,
    };
    const counts = new Map<string, number>();
    for (const a of accs) counts.set(a.status, (counts.get(a.status) ?? 0) + 1);
    return [...counts.entries()].map(([name, value]) => ({
      name,
      value,
      color: palette[name] ?? theme.palette.primary.main,
    }));
  }, [accs, theme]);

  const quickActions = [
    { label: 'Open Account', icon: <AddCardRoundedIcon />, to: '/accounts/new', roles: CAN.submitAccount },
    { label: 'Register Client', icon: <PersonAddRoundedIcon />, to: '/clients/new', roles: CAN.registerClient },
    { label: 'Create Product', icon: <AddCircleOutlineRoundedIcon />, to: '/products/new', roles: CAN.createProduct },
  ].filter((a) => hasRole(...(a.roles as Role[])));

  const recent = [...accs].slice(-6).reverse();

  return (
    <Box>
      {/* Greeting hero */}
      <Box mb={4}>
        <Typography variant="h4">
          {greeting()}, {user?.fullName?.split(' ')[0] ?? 'there'}
        </Typography>
        <Typography color="text.secondary" mt={0.5}>
          {user?.primaryRole ? ROLE_LABELS[user.primaryRole] : ''} ·{' '}
          {format(new Date(), 'EEEE, d MMMM yyyy')}
        </Typography>
      </Box>

      {/* Stat cards */}
      <Grid container spacing={2.5} mb={3}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            icon={<PeopleRoundedIcon />}
            label="Total Clients"
            value={clients?.length}
            hint={`${clients?.filter((c) => c.status === 'Active').length ?? 0} active`}
            tone="primary"
            loading={loadingClients}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            icon={<CategoryRoundedIcon />}
            label="Savings Products"
            value={products?.length}
            hint="Available to offer"
            tone="info"
            loading={loadingProducts}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            icon={<CheckCircleRoundedIcon />}
            label="Active Accounts"
            value={activeAccounts}
            hint={`${accs.length} total`}
            tone="success"
            loading={loadingAccounts}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            icon={<AccountBalanceWalletRoundedIcon />}
            label="Assets Under Management"
            value={totalBalance.toLocaleString(undefined, { maximumFractionDigits: 0 })}
            hint="Sum of all balances"
            tone="warning"
            loading={loadingAccounts}
          />
        </Grid>
      </Grid>

      {/* Charts */}
      <Grid container spacing={2.5} mb={3}>
        <Grid item xs={12} md={8}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle1">Accounts Opened</Typography>
              <Typography variant="caption" color="text.secondary">
                Applications submitted over recent months
              </Typography>
              <Box mt={2}>
                <AccountsTrendChart data={trend} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle1" mb={2}>
                Account Status
              </Typography>
              <StatusDonut data={donut} total={accs.length} />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Quick actions + recent */}
      <Grid container spacing={2.5}>
        {quickActions.length > 0 && (
          <Grid item xs={12} md={4}>
            <Card sx={{ height: '100%' }}>
              <CardContent>
                <Typography variant="subtitle1" mb={2}>
                  Quick Actions
                </Typography>
                <Stack spacing={1.25}>
                  {quickActions.map((a) => (
                    <Button
                      key={a.to}
                      variant="outlined"
                      startIcon={a.icon}
                      endIcon={<ArrowForwardRoundedIcon sx={{ ml: 'auto' }} />}
                      onClick={() => navigate(a.to)}
                      sx={{ justifyContent: 'flex-start', py: 1.25, '& .MuiButton-endIcon': { ml: 'auto' } }}
                    >
                      {a.label}
                    </Button>
                  ))}
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        )}

        <Grid item xs={12} md={quickActions.length > 0 ? 8 : 12}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                <Typography variant="subtitle1">Recent Applications</Typography>
                <Button size="small" onClick={() => navigate('/accounts')}>
                  View all
                </Button>
              </Box>
              <Divider sx={{ mb: 1 }} />
              {recent.length === 0 ? (
                <Typography color="text.secondary" variant="body2" py={2}>
                  No accounts yet.
                </Typography>
              ) : (
                recent.map((a) => (
                  <Box
                    key={a.id}
                    onClick={() => navigate(`/accounts/${a.id}`)}
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      py: 1.25,
                      px: 1,
                      mx: -1,
                      borderRadius: 2,
                      cursor: 'pointer',
                      '&:hover': { backgroundColor: 'action.hover' },
                    }}
                  >
                    <Box>
                      <Typography variant="body2" fontWeight={700}>
                        {a.accountNo}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {a.currencyCode} · {a.nominalAnnualRate}% p.a.
                      </Typography>
                    </Box>
                    <Box display="flex" alignItems="center" gap={2}>
                      <Typography variant="body2" fontWeight={700} sx={tabularNums}>
                        {a.accountBalance.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                      </Typography>
                      <StatusChip status={a.status} size="small" />
                    </Box>
                  </Box>
                ))
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
