import {
  Box, Card, CardContent, Typography, Button, Grid, Skeleton, Alert,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useNavigate, useParams } from 'react-router-dom';
import { useProduct } from '../../api/products.api';
import { PageHeader } from '../../components/common/PageHeader';

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Box display="flex" justifyContent="space-between" py={1.5} borderBottom="1px solid" borderColor="divider">
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Typography variant="body2" fontWeight={500}>{value ?? '—'}</Typography>
    </Box>
  );
}

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: product, isLoading, isError } = useProduct(id!);

  return (
    <Box>
      <Button
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate('/products')}
        sx={{ mb: 2 }}
        variant="text"
        color="inherit"
      >
        Back to Products
      </Button>

      <PageHeader title={isLoading ? '…' : (product?.name ?? 'Product')} subtitle="Savings product details" />

      {isError && <Alert severity="error" sx={{ mb: 2 }}>Failed to load product.</Alert>}

      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" mb={2}>Product Details</Typography>
              {isLoading ? (
                Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} height={40} />)
              ) : (
                <>
                  <InfoRow label="Name" value={product?.name} />
                  <InfoRow label="Short Name" value={product?.shortName} />
                  <InfoRow label="Currency" value={product?.currencyCode} />
                  <InfoRow label="Decimal Places" value={product?.currencyDecimalPlaces} />
                  <InfoRow label="Nominal Annual Rate" value={`${product?.nominalAnnualRate}%`} />
                  <InfoRow label="Status" value={product?.status} />
                </>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
