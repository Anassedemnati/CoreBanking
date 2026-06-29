import {
  Box, Card, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Typography, Button, Skeleton, Alert,
  TextField, InputAdornment, Link,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import SearchIcon from '@mui/icons-material/Search';
import AccountBalanceRoundedIcon from '@mui/icons-material/AccountBalanceRounded';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAccounts } from '../../api/accounts.api';
import { useClients } from '../../api/clients.api';
import { StatusChip } from '../../components/common/StatusChip';
import { PageHeader } from '../../components/common/PageHeader';
import { RoleGuard } from '../../components/common/RoleGuard';
import { EmptyState } from '../../components/common/EmptyState';
import { CAN } from '../../auth/roles';
import { tabularNums } from '../../theme/theme';

export default function AccountsPage() {
  const navigate = useNavigate();
  const { data: accounts, isLoading, isError } = useAccounts();
  const { data: clients } = useClients();
  const [search, setSearch] = useState('');

  const clientName = new Map((clients ?? []).map((c) => [c.id, c.displayName]));

  const filtered = accounts?.filter(
    (a) =>
      a.accountNo.toLowerCase().includes(search.toLowerCase()) ||
      a.status.toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <Box>
      <PageHeader
        title="Savings Accounts"
        subtitle={`${accounts?.length ?? 0} accounts`}
        actions={
          <RoleGuard roles={CAN.submitAccount}>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => navigate('/accounts/new')}
            >
              Open Account
            </Button>
          </RoleGuard>
        }
      />

      <Card>
        <Box p={2}>
          <TextField
            placeholder="Search by account number or status…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            }}
            sx={{ width: 360 }}
          />
        </Box>

        {isError && <Alert severity="error" sx={{ mx: 2, mb: 2 }}>Failed to load accounts.</Alert>}

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Account No.</TableCell>
                <TableCell>Client</TableCell>
                <TableCell>Currency</TableCell>
                <TableCell align="right">Balance</TableCell>
                <TableCell align="right">Rate</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Submitted On</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading
                ? Array.from({ length: 6 }).map((_, i) => (
                    <TableRow key={i}>
                      {Array.from({ length: 8 }).map((__, j) => (
                        <TableCell key={j}><Skeleton /></TableCell>
                      ))}
                    </TableRow>
                  ))
                : (filtered ?? []).map((a) => (
                    <TableRow key={a.id} hover>
                      <TableCell>
                        <Typography variant="body2" fontWeight={600}>{a.accountNo}</Typography>
                      </TableCell>
                      <TableCell>
                        <Link
                          component="button"
                          type="button"
                          onClick={() => navigate(`/clients/${a.clientId}`)}
                          sx={{ fontWeight: 500 }}
                        >
                          {clientName.get(a.clientId) ?? '—'}
                        </Link>
                      </TableCell>
                      <TableCell>{a.currencyCode}</TableCell>
                      <TableCell align="right">
                        <Typography variant="body2" fontWeight={700} sx={tabularNums}>
                          {a.accountBalance.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">{a.nominalAnnualRate}%</TableCell>
                      <TableCell><StatusChip status={a.status} /></TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">{a.submittedOn}</Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Button size="small" onClick={() => navigate(`/accounts/${a.id}`)}>
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
              {!isLoading && filtered?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8} sx={{ borderBottom: 'none' }}>
                    <EmptyState
                      icon={<AccountBalanceRoundedIcon />}
                      title={search ? 'No matching accounts' : 'No accounts yet'}
                      description={
                        search
                          ? 'Try a different account number or status.'
                          : 'Opened savings accounts will appear here.'
                      }
                    />
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>
    </Box>
  );
}
