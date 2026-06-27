import { Box, Grid, Card, CardContent, Typography, Skeleton, Divider } from '@mui/material';
import PeopleIcon from '@mui/icons-material/People';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import CategoryIcon from '@mui/icons-material/Category';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import { useClients } from '../api/clients.api';
import { useProducts } from '../api/products.api';
import { useAccounts } from '../api/accounts.api';
import { useAuth } from '../auth/AuthProvider';
import { ROLE_LABELS } from '../auth/AuthProvider';

interface StatCardProps {
  icon: React.ReactNode;
  label: string;
  value: number | undefined;
  color: string;
  loading: boolean;
}

function StatCard({ icon, label, value, color, loading }: StatCardProps) {
  return (
    <Card>
      <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 2.5 }}>
        <Box
          sx={{
            bgcolor: color,
            borderRadius: 2,
            p: 1.5,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          {icon}
        </Box>
        <Box>
          <Typography variant="body2" color="text.secondary">
            {label}
          </Typography>
          {loading ? (
            <Skeleton width={40} height={32} />
          ) : (
            <Typography variant="h5" fontWeight={700}>
              {value ?? '—'}
            </Typography>
          )}
        </Box>
      </CardContent>
    </Card>
  );
}

export default function DashboardPage() {
  const { user } = useAuth();
  const { data: clients, isLoading: loadingClients } = useClients();
  const { data: products, isLoading: loadingProducts } = useProducts();
  const { data: accounts, isLoading: loadingAccounts } = useAccounts();

  const activeAccounts = accounts?.filter((a) => a.status === 'Active').length;
  const pendingAccounts = accounts?.filter(
    (a) => a.status === 'Submitted' || a.status === 'Approved',
  ).length;

  return (
    <Box>
      {/* Greeting */}
      <Box mb={4}>
        <Typography variant="h4">Good morning, {user?.fullName} 👋</Typography>
        <Typography color="text.secondary" mt={0.5}>
          {user?.primaryRole ? ROLE_LABELS[user.primaryRole] : ''} — CoreBanking Staff Portal
        </Typography>
      </Box>

      {/* Stats row */}
      <Grid container spacing={2} mb={4}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            icon={<PeopleIcon sx={{ color: '#0D47A1' }} />}
            label="Total Clients"
            value={clients?.length}
            color="#EFF6FF"
            loading={loadingClients}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            icon={<CategoryIcon sx={{ color: '#7C3AED' }} />}
            label="Savings Products"
            value={products?.length}
            color="#F5F3FF"
            loading={loadingProducts}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            icon={<CheckCircleIcon sx={{ color: '#15803D' }} />}
            label="Active Accounts"
            value={activeAccounts}
            color="#F0FDF4"
            loading={loadingAccounts}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            icon={<AccountBalanceIcon sx={{ color: '#D97706' }} />}
            label="Pending Accounts"
            value={pendingAccounts}
            color="#FFFBEB"
            loading={loadingAccounts}
          />
        </Grid>
      </Grid>

      {/* Recent accounts */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" mb={2}>
            Recent Account Applications
          </Typography>
          <Divider sx={{ mb: 2 }} />
          {loadingAccounts ? (
            Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} height={36} sx={{ mb: 1 }} />)
          ) : (
            <Box>
              {(accounts ?? []).slice(-5).reverse().map((a) => (
                <Box
                  key={a.id}
                  display="flex"
                  justifyContent="space-between"
                  alignItems="center"
                  py={1}
                  borderBottom="1px solid"
                  borderColor="divider"
                >
                  <Box>
                    <Typography variant="body2" fontWeight={600}>
                      {a.accountNo}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {a.currencyCode} · {a.nominalAnnualRate}% p.a.
                    </Typography>
                  </Box>
                  <Box textAlign="right">
                    <Typography variant="body2" fontWeight={600}>
                      {a.accountBalance.toLocaleString(undefined, {
                        minimumFractionDigits: 2,
                      })}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {a.status}
                    </Typography>
                  </Box>
                </Box>
              ))}
              {!accounts?.length && (
                <Typography color="text.secondary" variant="body2">
                  No accounts yet.
                </Typography>
              )}
            </Box>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
