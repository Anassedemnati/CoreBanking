import {
  Box, Card, CardContent, Button,
  TextField, Alert, CircularProgress,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useRegisterClient } from '../../api/clients.api';
import { PageHeader } from '../../components/common/PageHeader';

interface FormValues {
  displayName: string;
  externalId: string;
}

export default function NewClientPage() {
  const navigate = useNavigate();
  const { mutate, isPending, error } = useRegisterClient();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>();

  const onSubmit = (values: FormValues) => {
    mutate(
      { displayName: values.displayName, externalId: values.externalId || undefined },
      { onSuccess: (data) => navigate(`/clients/${data.id}`) },
    );
  };

  const apiError =
    error && (error as { response?: { data?: { detail?: string } } }).response?.data?.detail;

  return (
    <Box maxWidth={560}>
      <Button
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate('/clients')}
        sx={{ mb: 2 }}
        variant="text"
        color="inherit"
      >
        Back
      </Button>

      <PageHeader title="Register New Client" subtitle="Create a client profile in the system" />

      {apiError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {apiError}
        </Alert>
      )}

      <Card>
        <CardContent>
          <Box
            component="form"
            onSubmit={handleSubmit(onSubmit)}
            display="flex"
            flexDirection="column"
            gap={2.5}
          >
            <TextField
              label="Full Name *"
              placeholder="e.g. Fatima Al-Hassan"
              error={!!errors.displayName}
              helperText={errors.displayName?.message ?? "The client's full legal name"}
              {...register('displayName', {
                required: 'Full name is required',
                maxLength: { value: 150, message: 'Max 150 characters' },
              })}
            />

            <TextField
              label="External ID"
              placeholder="e.g. National ID, passport number…"
              helperText="Optional reference number from an external system"
              {...register('externalId')}
            />

            <Box display="flex" gap={2} justifyContent="flex-end" pt={1}>
              <Button variant="outlined" onClick={() => navigate('/clients')} disabled={isPending}>
                Cancel
              </Button>
              <Button
                type="submit"
                variant="contained"
                disabled={isPending}
                startIcon={isPending ? <CircularProgress size={16} color="inherit" /> : undefined}
              >
                Register Client
              </Button>
            </Box>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
