import {
  Box, Card, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Typography, Button, TextField,
  InputAdornment, Skeleton, Alert,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import SearchIcon from '@mui/icons-material/Search';
import PeopleRoundedIcon from '@mui/icons-material/PeopleRounded';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useClients } from '../../api/clients.api';
import { StatusChip } from '../../components/common/StatusChip';
import { PageHeader } from '../../components/common/PageHeader';
import { RoleGuard } from '../../components/common/RoleGuard';
import { EmptyState } from '../../components/common/EmptyState';
import { CAN } from '../../auth/roles';

export default function ClientsPage() {
  const navigate = useNavigate();
  const { data: clients, isLoading, isError } = useClients();
  const [search, setSearch] = useState('');

  const filtered = clients?.filter((c) =>
    c.displayName.toLowerCase().includes(search.toLowerCase()) ||
    (c.externalId ?? '').toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <Box>
      <PageHeader
        title="Clients"
        subtitle={`${clients?.length ?? 0} clients registered`}
        actions={
          <RoleGuard roles={CAN.registerClient}>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => navigate('/clients/new')}
            >
              Register Client
            </Button>
          </RoleGuard>
        }
      />

      <Card>
        <Box p={2}>
          <TextField
            placeholder="Search by name or ID…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            InputProps={{ startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> }}
            sx={{ width: 320 }}
          />
        </Box>

        {isError && <Alert severity="error" sx={{ mx: 2, mb: 2 }}>Failed to load clients.</Alert>}

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>External ID</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Activation Date</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading
                ? Array.from({ length: 5 }).map((_, i) => (
                    <TableRow key={i}>
                      {Array.from({ length: 5 }).map((__, j) => (
                        <TableCell key={j}><Skeleton /></TableCell>
                      ))}
                    </TableRow>
                  ))
                : (filtered ?? []).map((c) => (
                    <TableRow key={c.id} hover>
                      <TableCell>
                        <Typography variant="body2" fontWeight={600}>{c.displayName}</Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">{c.externalId ?? '—'}</Typography>
                      </TableCell>
                      <TableCell><StatusChip status={c.status} variant="client" /></TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {c.activationDate ?? '—'}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Button size="small" onClick={() => navigate(`/clients/${c.id}`)}>
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
              {!isLoading && filtered?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} sx={{ borderBottom: 'none' }}>
                    <EmptyState
                      icon={<PeopleRoundedIcon />}
                      title={search ? 'No matching clients' : 'No clients yet'}
                      description={
                        search
                          ? 'Try a different name or external ID.'
                          : 'Registered clients will appear here.'
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
