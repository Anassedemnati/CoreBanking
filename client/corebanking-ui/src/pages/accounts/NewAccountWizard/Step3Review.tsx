import { Box, Grid, Typography, Card, CardContent, Divider, TextField, Alert } from '@mui/material';
import type { ClientDto, SavingsProductDto } from '../../../api/types';

interface Props {
  client: ClientDto;
  product: SavingsProductDto;
  accountNo: string;
  submittedOn: string;
  onAccountNoChange: (v: string) => void;
  onDateChange: (v: string) => void;
  apiError?: string | null;
}

export function Step3Review({
  client, product, accountNo, submittedOn, onAccountNoChange, onDateChange, apiError,
}: Props) {
  return (
    <Box>
      <Typography variant="h6" gutterBottom>Review & Submit</Typography>
      <Typography variant="body2" color="text.secondary" mb={3}>
        Verify the details below and enter the account number before submitting.
      </Typography>

      {apiError && <Alert severity="error" sx={{ mb: 2 }}>{apiError}</Alert>}

      <Grid container spacing={3} mb={3}>
        <Grid item xs={12} sm={6}>
          <Card variant="outlined">
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>Client</Typography>
              <Typography variant="body1" fontWeight={600}>{client.displayName}</Typography>
              {client.externalId && (
                <Typography variant="body2" color="text.secondary">ID: {client.externalId}</Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6}>
          <Card variant="outlined">
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>Product</Typography>
              <Typography variant="body1" fontWeight={600}>{product.name}</Typography>
              <Typography variant="body2" color="text.secondary">
                {product.currencyCode} · {product.nominalAnnualRate}% p.a.
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Divider sx={{ mb: 3 }} />

      <Grid container spacing={2}>
        <Grid item xs={12} sm={6}>
          <TextField
            label="Account Number *"
            value={accountNo}
            onChange={(e) => onAccountNoChange(e.target.value)}
            fullWidth
            helperText="Unique account number, max 50 characters"
            inputProps={{ maxLength: 50 }}
            required
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <TextField
            label="Submission Date *"
            type="date"
            value={submittedOn}
            onChange={(e) => onDateChange(e.target.value)}
            fullWidth
            InputLabelProps={{ shrink: true }}
            required
          />
        </Grid>
      </Grid>
    </Box>
  );
}
