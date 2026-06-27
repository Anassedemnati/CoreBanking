import {
  Box, Card, CardContent, Typography, Radio, Grid, Skeleton, Alert, Chip,
} from '@mui/material';
import { useProducts } from '../../../api/products.api';
import type { SavingsProductDto } from '../../../api/types';

interface Props {
  selected: SavingsProductDto | null;
  onSelect: (product: SavingsProductDto) => void;
}

export function Step2ProductSelect({ selected, onSelect }: Props) {
  const { data: products, isLoading, isError } = useProducts();

  return (
    <Box>
      <Typography variant="h6" gutterBottom>Select Savings Product</Typography>
      <Typography variant="body2" color="text.secondary" mb={3}>
        The account will inherit the product's interest settings and currency.
      </Typography>

      {isError && <Alert severity="error" sx={{ mb: 2 }}>Failed to load products.</Alert>}

      {isLoading ? (
        <Grid container spacing={2}>
          {Array.from({ length: 4 }).map((_, i) => (
            <Grid item xs={12} sm={6} key={i}>
              <Skeleton variant="rounded" height={120} />
            </Grid>
          ))}
        </Grid>
      ) : (
        <Grid container spacing={2}>
          {(products ?? []).map((p) => (
            <Grid item xs={12} sm={6} key={p.id}>
              <Card
                variant="outlined"
                onClick={() => onSelect(p)}
                sx={{
                  cursor: 'pointer',
                  border: '2px solid',
                  borderColor: selected?.id === p.id ? 'primary.main' : 'divider',
                  transition: 'border-color 0.15s',
                  '&:hover': { borderColor: 'primary.light' },
                }}
              >
                <CardContent sx={{ display: 'flex', alignItems: 'flex-start', gap: 1, p: 2 }}>
                  <Radio
                    checked={selected?.id === p.id}
                    size="small"
                    sx={{ mt: -0.5, p: 0 }}
                  />
                  <Box flexGrow={1}>
                    <Typography variant="subtitle2">{p.name}</Typography>
                    <Typography variant="caption" color="text.secondary">{p.shortName}</Typography>
                    <Box display="flex" gap={1} flexWrap="wrap" mt={1}>
                      <Chip label={`${p.currencyCode}`} size="small" variant="outlined" />
                      <Chip label={`${p.nominalAnnualRate}% p.a.`} size="small" color="primary" variant="outlined" />
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))}
          {!products?.length && (
            <Grid item xs={12}>
              <Alert severity="warning">No savings products available. Ask an Operations Officer to create one first.</Alert>
            </Grid>
          )}
        </Grid>
      )}

      {selected && (
        <Alert severity="success" sx={{ mt: 2 }}>
          Selected: <strong>{selected.name}</strong> — {selected.nominalAnnualRate}% p.a. · {selected.currencyCode}
        </Alert>
      )}
    </Box>
  );
}
