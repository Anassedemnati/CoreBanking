import {
  Box, Card, Typography, TextField,
  InputAdornment, Table, TableBody, TableCell, TableHead,
  TableRow, Radio, Skeleton, Alert,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import { useState } from 'react';
import { useClients } from '../../../api/clients.api';
import type { ClientDto } from '../../../api/types';

interface Props {
  selected: ClientDto | null;
  onSelect: (client: ClientDto) => void;
}

export function Step1ClientSelect({ selected, onSelect }: Props) {
  const { data: clients, isLoading, isError } = useClients();
  const [search, setSearch] = useState('');

  const activeClients = clients?.filter((c) => c.status === 'Active');
  const filtered = activeClients?.filter(
    (c) =>
      c.displayName.toLowerCase().includes(search.toLowerCase()) ||
      (c.externalId ?? '').toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <Box>
      <Typography variant="h6" gutterBottom>Select Client</Typography>
      <Typography variant="body2" color="text.secondary" mb={3}>
        Only active clients can open a savings account.
      </Typography>

      {isError && <Alert severity="error" sx={{ mb: 2 }}>Failed to load clients.</Alert>}

      <TextField
        placeholder="Search active clients…"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        size="small"
        InputProps={{
          startAdornment: (
            <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment>
          ),
        }}
        sx={{ mb: 2, width: 320 }}
      />

      <Card variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell padding="checkbox" />
              <TableCell>Name</TableCell>
              <TableCell>External ID</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading
              ? Array.from({ length: 4 }).map((_, i) => (
                  <TableRow key={i}>
                    <TableCell padding="checkbox"><Radio disabled /></TableCell>
                    <TableCell><Skeleton /></TableCell>
                    <TableCell><Skeleton /></TableCell>
                  </TableRow>
                ))
              : (filtered ?? []).map((c) => (
                  <TableRow
                    key={c.id}
                    hover
                    selected={selected?.id === c.id}
                    onClick={() => onSelect(c)}
                    sx={{ cursor: 'pointer' }}
                  >
                    <TableCell padding="checkbox">
                      <Radio checked={selected?.id === c.id} size="small" />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" fontWeight={600}>{c.displayName}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" color="text.secondary">{c.externalId ?? '—'}</Typography>
                    </TableCell>
                  </TableRow>
                ))}
            {!isLoading && filtered?.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} align="center" sx={{ py: 3 }}>
                  <Typography color="text.secondary" variant="body2">No active clients found.</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Card>

      {selected && (
        <Alert severity="success" sx={{ mt: 2 }}>
          Selected: <strong>{selected.displayName}</strong>
        </Alert>
      )}
    </Box>
  );
}
