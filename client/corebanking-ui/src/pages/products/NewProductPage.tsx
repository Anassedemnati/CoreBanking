import {
  Box, Card, CardContent, Typography, Button, TextField,
  Alert, CircularProgress, Grid, MenuItem,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useForm, Controller } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useCreateProduct } from '../../api/products.api';
import { PageHeader } from '../../components/common/PageHeader';
import {
  COMPOUNDING_OPTIONS,
  POSTING_PERIOD_OPTIONS,
  CALCULATION_TYPE_OPTIONS,
  DAYS_IN_YEAR_OPTIONS,
  type CreateSavingsProductRequest,
} from '../../api/types';

export default function NewProductPage() {
  const navigate = useNavigate();
  const { mutate, isPending, error } = useCreateProduct();

  const { register, control, handleSubmit, formState: { errors } } =
    useForm<CreateSavingsProductRequest>({
      defaultValues: {
        compoundingPeriod: 4,
        postingPeriod: 4,
        calculationType: 1,
        daysInYearType: 365,
        currencyDecimalPlaces: 2,
      },
    });

  const onSubmit = (values: CreateSavingsProductRequest) => {
    mutate(values, { onSuccess: (data) => navigate(`/products/${data.id}`) });
  };

  const apiError =
    error && (error as { response?: { data?: { detail?: string } } }).response?.data?.detail;

  return (
    <Box maxWidth={720}>
      <Button
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate('/products')}
        sx={{ mb: 2 }}
        variant="text"
        color="inherit"
      >
        Back
      </Button>

      <PageHeader title="Create Savings Product" subtitle="Define a new savings product template" />

      {apiError && <Alert severity="error" sx={{ mb: 2 }}>{apiError}</Alert>}

      <Card>
        <CardContent>
          <Box component="form" onSubmit={handleSubmit(onSubmit)}>
            <Typography variant="subtitle2" gutterBottom>Basic Information</Typography>
            <Grid container spacing={2} mb={3}>
              <Grid item xs={12} sm={8}>
                <TextField
                  fullWidth
                  label="Product Name *"
                  error={!!errors.name}
                  helperText={errors.name?.message}
                  {...register('name', { required: 'Name is required', maxLength: { value: 150, message: 'Max 150 characters' } })}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth
                  label="Short Name *"
                  error={!!errors.shortName}
                  helperText={errors.shortName?.message}
                  {...register('shortName', { required: 'Short name is required', maxLength: { value: 50, message: 'Max 50 chars' } })}
                />
              </Grid>
              <Grid item xs={6} sm={4}>
                <TextField
                  fullWidth
                  label="Currency Code *"
                  placeholder="USD"
                  inputProps={{ maxLength: 3, style: { textTransform: 'uppercase' } }}
                  error={!!errors.currencyCode}
                  helperText={errors.currencyCode?.message ?? 'ISO 4217, e.g. USD'}
                  {...register('currencyCode', { required: 'Required', minLength: { value: 3, message: '3 letters' }, maxLength: { value: 3, message: '3 letters' } })}
                />
              </Grid>
              <Grid item xs={6} sm={4}>
                <TextField
                  fullWidth
                  label="Decimal Places *"
                  type="number"
                  inputProps={{ min: 0, max: 6 }}
                  {...register('currencyDecimalPlaces', { required: true, valueAsNumber: true })}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth
                  label="Nominal Annual Rate (%) *"
                  type="number"
                  inputProps={{ min: 0, step: 0.01 }}
                  error={!!errors.nominalAnnualRate}
                  helperText={errors.nominalAnnualRate?.message}
                  {...register('nominalAnnualRate', { required: 'Required', valueAsNumber: true, min: { value: 0, message: 'Must be ≥ 0' } })}
                />
              </Grid>
            </Grid>

            <Typography variant="subtitle2" gutterBottom>Interest Settings</Typography>
            <Grid container spacing={2} mb={3}>
              <Grid item xs={12} sm={6}>
                <Controller
                  name="compoundingPeriod"
                  control={control}
                  render={({ field }) => (
                    <TextField select fullWidth label="Compounding Period" {...field}>
                      {COMPOUNDING_OPTIONS.map((o) => (
                        <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Controller
                  name="postingPeriod"
                  control={control}
                  render={({ field }) => (
                    <TextField select fullWidth label="Posting Period" {...field}>
                      {POSTING_PERIOD_OPTIONS.map((o) => (
                        <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Controller
                  name="calculationType"
                  control={control}
                  render={({ field }) => (
                    <TextField select fullWidth label="Calculation Type" {...field}>
                      {CALCULATION_TYPE_OPTIONS.map((o) => (
                        <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Controller
                  name="daysInYearType"
                  control={control}
                  render={({ field }) => (
                    <TextField select fullWidth label="Days in Year" {...field}>
                      {DAYS_IN_YEAR_OPTIONS.map((o) => (
                        <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              </Grid>
            </Grid>

            <Box display="flex" gap={2} justifyContent="flex-end">
              <Button variant="outlined" onClick={() => navigate('/products')} disabled={isPending}>
                Cancel
              </Button>
              <Button
                type="submit"
                variant="contained"
                disabled={isPending}
                startIcon={isPending ? <CircularProgress size={16} color="inherit" /> : undefined}
              >
                Create Product
              </Button>
            </Box>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
