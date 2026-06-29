import {
  Box, Stepper, Step, StepLabel, Button, Card, CardContent,
  CircularProgress,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useState } from 'react';
import { format } from 'date-fns';
import { useNavigate } from 'react-router-dom';
import { Step1ClientSelect } from './Step1ClientSelect';
import { Step2ProductSelect } from './Step2ProductSelect';
import { Step3Review } from './Step3Review';
import { useSubmitAccount } from '../../../api/accounts.api';
import { PageHeader } from '../../../components/common/PageHeader';
import type { ClientDto, SavingsProductDto } from '../../../api/types';

const STEPS = ['Select Client', 'Select Product', 'Review & Submit'];

export default function NewAccountWizard() {
  const navigate = useNavigate();
  const { mutate, isPending, error } = useSubmitAccount();

  const [activeStep, setActiveStep] = useState(0);
  const [selectedClient, setSelectedClient] = useState<ClientDto | null>(null);
  const [selectedProduct, setSelectedProduct] = useState<SavingsProductDto | null>(null);
  const [submittedOn, setSubmittedOn] = useState(format(new Date(), 'yyyy-MM-dd'));

  const canNext =
    (activeStep === 0 && !!selectedClient) ||
    (activeStep === 1 && !!selectedProduct) ||
    (activeStep === 2 && !!submittedOn);

  const handleNext = () => {
    if (activeStep < STEPS.length - 1) {
      setActiveStep((s) => s + 1);
    } else {
      handleSubmit();
    }
  };

  const handleSubmit = () => {
    if (!selectedClient || !selectedProduct) return;
    mutate(
      {
        clientId: selectedClient.id,
        productId: selectedProduct.id,
        currencyCode: selectedProduct.currencyCode,
        currencyDecimalPlaces: selectedProduct.currencyDecimalPlaces,
        nominalAnnualRate: selectedProduct.nominalAnnualRate,
        submittedOn,
      },
      { onSuccess: (data) => navigate(`/accounts/${data.id}`) },
    );
  };

  const apiError =
    error && (error as { response?: { data?: { detail?: string } } }).response?.data?.detail;

  return (
    <Box maxWidth={800}>
      <Button
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate('/accounts')}
        sx={{ mb: 2 }}
        variant="text"
        color="inherit"
      >
        Back
      </Button>

      <PageHeader title="Open Savings Account" subtitle="Complete the 3-step process to submit a new account application" />

      {/* Stepper */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stepper activeStep={activeStep} alternativeLabel>
            {STEPS.map((label) => (
              <Step key={label}>
                <StepLabel>{label}</StepLabel>
              </Step>
            ))}
          </Stepper>
        </CardContent>
      </Card>

      {/* Step content */}
      <Card sx={{ mb: 3 }}>
        <CardContent sx={{ p: 3 }}>
          {activeStep === 0 && (
            <Step1ClientSelect selected={selectedClient} onSelect={setSelectedClient} />
          )}
          {activeStep === 1 && (
            <Step2ProductSelect selected={selectedProduct} onSelect={setSelectedProduct} />
          )}
          {activeStep === 2 && selectedClient && selectedProduct && (
            <Step3Review
              client={selectedClient}
              product={selectedProduct}
              submittedOn={submittedOn}
              onDateChange={setSubmittedOn}
              apiError={apiError}
            />
          )}
        </CardContent>
      </Card>

      {/* Navigation */}
      <Box display="flex" justifyContent="space-between">
        <Button
          variant="outlined"
          onClick={() => setActiveStep((s) => Math.max(0, s - 1))}
          disabled={activeStep === 0}
        >
          Back
        </Button>

        <Button
          variant="contained"
          onClick={handleNext}
          disabled={!canNext || isPending}
          startIcon={isPending ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          {activeStep === STEPS.length - 1 ? 'Submit Application' : 'Next'}
        </Button>
      </Box>
    </Box>
  );
}
