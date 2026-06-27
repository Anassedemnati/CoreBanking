import {
  Box, Card, CardContent, Typography, Button, Grid, Skeleton, Alert,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import { useNavigate, useParams } from 'react-router-dom';
import { format } from 'date-fns';
import { useClient, useActivateClient } from '../../api/clients.api';
import { StatusChip } from '../../components/common/StatusChip';
import { PageHeader } from '../../components/common/PageHeader';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { RoleGuard } from '../../components/common/RoleGuard';
import { CAN } from '../../auth/roles';
import { useState } from 'react';

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Box display="flex" justifyContent="space-between" py={1.5} borderBottom="1px solid" borderColor="divider">
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Typography variant="body2" fontWeight={500}>{value ?? '—'}</Typography>
    </Box>
  );
}

export default function ClientDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: client, isLoading, isError } = useClient(id!);
  const activateMutation = useActivateClient();
  const [confirmOpen, setConfirmOpen] = useState(false);

  const handleActivate = () => {
    const today = format(new Date(), 'yyyy-MM-dd');
    activateMutation.mutate(
      { id: id!, activationDate: today },
      { onSuccess: () => setConfirmOpen(false) },
    );
  };

  return (
    <Box>
      <Button
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate('/clients')}
        sx={{ mb: 2 }}
        variant="text"
        color="inherit"
      >
        Back to Clients
      </Button>

      <PageHeader
        title={isLoading ? '…' : (client?.displayName ?? 'Client')}
        subtitle="Client profile"
        actions={
          client?.status === 'Pending' ? (
            <RoleGuard roles={CAN.activateClient}>
              <Button
                variant="contained"
                color="success"
                startIcon={<CheckCircleIcon />}
                onClick={() => setConfirmOpen(true)}
              >
                Activate Client
              </Button>
            </RoleGuard>
          ) : null
        }
      />

      {isError && <Alert severity="error" sx={{ mb: 2 }}>Failed to load client.</Alert>}

      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" mb={2}>Details</Typography>
              {isLoading ? (
                Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} height={40} />)
              ) : (
                <>
                  <InfoRow label="Full Name" value={client?.displayName} />
                  <InfoRow label="External ID" value={client?.externalId} />
                  <InfoRow
                    label="Status"
                    value={client ? <StatusChip status={client.status} variant="client" /> : null}
                  />
                  <InfoRow label="Activation Date" value={client?.activationDate} />
                </>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <ConfirmDialog
        open={confirmOpen}
        title="Activate Client"
        message={`Are you sure you want to activate ${client?.displayName}? Their activation date will be set to today.`}
        confirmLabel="Activate"
        confirmColor="primary"
        loading={activateMutation.isPending}
        onConfirm={handleActivate}
        onCancel={() => setConfirmOpen(false)}
      />
    </Box>
  );
}
