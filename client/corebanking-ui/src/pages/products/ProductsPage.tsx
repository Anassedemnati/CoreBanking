import {
  Box, Card, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Typography, Button, Skeleton, Alert,
  TextField, InputAdornment,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import SearchIcon from '@mui/icons-material/Search';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useProducts } from '../../api/products.api';
import { PageHeader } from '../../components/common/PageHeader';
import { RoleGuard } from '../../components/common/RoleGuard';
import { CAN } from '../../auth/roles';

export default function ProductsPage() {
  const navigate = useNavigate();
  const { data: products, isLoading, isError } = useProducts();
  const [search, setSearch] = useState('');

  const filtered = products?.filter(
    (p) =>
      p.name.toLowerCase().includes(search.toLowerCase()) ||
      p.shortName.toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <Box>
      <PageHeader
        title="Savings Products"
        subtitle={`${products?.length ?? 0} products`}
        actions={
          <RoleGuard roles={CAN.createProduct}>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => navigate('/products/new')}
            >
              Create Product
            </Button>
          </RoleGuard>
        }
      />

      <Card>
        <Box p={2}>
          <TextField
            placeholder="Search products…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            }}
            sx={{ width: 320 }}
          />
        </Box>

        {isError && <Alert severity="error" sx={{ mx: 2, mb: 2 }}>Failed to load products.</Alert>}

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Short Name</TableCell>
                <TableCell>Currency</TableCell>
                <TableCell align="right">Interest Rate</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading
                ? Array.from({ length: 4 }).map((_, i) => (
                    <TableRow key={i}>
                      {Array.from({ length: 6 }).map((__, j) => (
                        <TableCell key={j}><Skeleton /></TableCell>
                      ))}
                    </TableRow>
                  ))
                : (filtered ?? []).map((p) => (
                    <TableRow key={p.id} hover>
                      <TableCell>
                        <Typography variant="body2" fontWeight={600}>{p.name}</Typography>
                      </TableCell>
                      <TableCell>{p.shortName}</TableCell>
                      <TableCell>{p.currencyCode}</TableCell>
                      <TableCell align="right">
                        <Typography variant="body2" fontWeight={600}>
                          {p.nominalAnnualRate}%
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">{p.status}</Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Button size="small" onClick={() => navigate(`/products/${p.id}`)}>
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
              {!isLoading && filtered?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                    <Typography color="text.secondary">No products found.</Typography>
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
