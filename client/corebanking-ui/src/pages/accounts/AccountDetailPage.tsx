import {
  Box, Card, CardContent, Grid, Typography, Button, Divider,
  Alert, Skeleton, TextField, Dialog, DialogTitle,
  DialogContent, DialogActions, CircularProgress, Link,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import CheckIcon from '@mui/icons-material/Check';
import CloseIcon from '@mui/icons-material/Close';
import BlockIcon from '@mui/icons-material/Block';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import SavingsIcon from '@mui/icons-material/Savings';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import { useState } from 'react';
import { format } from 'date-fns';
import { useNavigate, useParams } from 'react-router-dom';
import { useAccount, useAccountTransactions, useApproveAccount, useActivateAccount, useRejectAccount, useWithdrawAccount, useCloseAccount, usePostInterest, useDeposit, useWithdrawMoney } from '../../api/accounts.api';
import { useClient } from '../../api/clients.api';
import { useProduct } from '../../api/products.api';
import { StatusChip } from '../../components/common/StatusChip';
import { PageHeader } from '../../components/common/PageHeader';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { RoleGuard } from '../../components/common/RoleGuard';
import { CAN } from '../../auth/roles';
import { TRANSACTION_TYPE_LABELS } from '../../api/types';
import { tabularNums } from '../../theme/theme';

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Box display="flex" justifyContent="space-between" py={1.5} borderBottom="1px solid" borderColor="divider">
      <Typography variant="body2" color="text.secondary" flexShrink={0} mr={2}>{label}</Typography>
      <Typography variant="body2" fontWeight={500} textAlign="right">{value ?? '—'}</Typography>
    </Box>
  );
}

interface TransactionDialogProps {
  open: boolean;
  type: 'deposit' | 'withdraw';
  accountId: string;
  onClose: () => void;
}

function TransactionDialog({ open, type, accountId, onClose }: TransactionDialogProps) {
  const [amount, setAmount] = useState('');
  const [date, setDate] = useState(format(new Date(), 'yyyy-MM-dd'));
  const depositMutation = useDeposit();
  const withdrawMutation = useWithdrawMoney();
  const mutation = type === 'deposit' ? depositMutation : withdrawMutation;

  const handleSubmit = () => {
    mutation.mutate(
      { id: accountId, transactionDate: date, amount: parseFloat(amount) },
      { onSuccess: () => { setAmount(''); onClose(); } },
    );
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{type === 'deposit' ? 'Deposit Funds' : 'Withdraw Funds'}</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: '16px !important' }}>
        <TextField
          label="Amount *"
          type="number"
          inputProps={{ min: 0.01, step: 0.01 }}
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          fullWidth
        />
        <TextField
          label="Transaction Date *"
          type="date"
          value={date}
          onChange={(e) => setDate(e.target.value)}
          InputLabelProps={{ shrink: true }}
          fullWidth
        />
        {mutation.error && (
          <Alert severity="error">
            {(mutation.error as { response?: { data?: { detail?: string } } }).response?.data?.detail ?? 'Operation failed'}
          </Alert>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2, gap: 1 }}>
        <Button variant="outlined" onClick={onClose} disabled={mutation.isPending}>Cancel</Button>
        <Button
          variant="contained"
          color={type === 'deposit' ? 'primary' : 'warning'}
          onClick={handleSubmit}
          disabled={!amount || mutation.isPending}
          startIcon={mutation.isPending ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          {type === 'deposit' ? 'Deposit' : 'Withdraw'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default function AccountDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: account, isLoading, isError } = useAccount(id!);
  const { data: transactions } = useAccountTransactions(id!);
  const { data: client } = useClient(account?.clientId ?? '');
  const { data: product } = useProduct(account?.productId ?? '');

  const approveMutation = useApproveAccount();
  const activateMutation = useActivateAccount();
  const rejectMutation = useRejectAccount();
  const withdrawMutation = useWithdrawAccount();
  const closeMutation = useCloseAccount();
  const postInterestMutation = usePostInterest();

  const [confirm, setConfirm] = useState<null | 'approve' | 'activate' | 'reject' | 'withdraw' | 'close' | 'postinterest'>(null);
  const [txDialog, setTxDialog] = useState<null | 'deposit' | 'withdraw'>(null);

  const today = format(new Date(), 'yyyy-MM-dd');

  const handleConfirm = () => {
    if (!confirm) return;
    const base = { id: id! };
    const handlers: Record<string, () => void> = {
      approve: () => approveMutation.mutate({ ...base, body: { approvedOn: today } }, { onSuccess: () => setConfirm(null) }),
      activate: () => activateMutation.mutate({ ...base, body: { activatedOn: today } }, { onSuccess: () => setConfirm(null) }),
      reject: () => rejectMutation.mutate({ ...base, body: { rejectedOn: today } }, { onSuccess: () => setConfirm(null) }),
      withdraw: () => withdrawMutation.mutate({ ...base, body: { withdrawnOn: today } }, { onSuccess: () => setConfirm(null) }),
      close: () => closeMutation.mutate({ ...base, body: { closedOn: today, withdrawBalance: true } }, { onSuccess: () => setConfirm(null) }),
      postinterest: () => postInterestMutation.mutate({ ...base, body: { asOf: today } }, { onSuccess: () => setConfirm(null) }),
    };
    handlers[confirm]?.();
  };

  const activeMutation =
    confirm === 'approve' ? approveMutation :
    confirm === 'activate' ? activateMutation :
    confirm === 'reject' ? rejectMutation :
    confirm === 'withdraw' ? withdrawMutation :
    confirm === 'close' ? closeMutation :
    postInterestMutation;

  const CONFIRM_META: Record<string, { title: string; message: string; color: 'primary' | 'error' | 'warning' }> = {
    approve: { title: 'Approve Account', message: 'Approve this savings account application?', color: 'primary' },
    activate: { title: 'Activate Account', message: 'Activate this account? It will become fully operational.', color: 'primary' },
    reject: { title: 'Reject Application', message: 'Reject this application? This action is irreversible.', color: 'error' },
    withdraw: { title: 'Withdraw Application', message: 'Mark this application as withdrawn? This is irreversible.', color: 'warning' },
    close: { title: 'Close Account', message: 'Close this account? Any remaining balance will be swept to zero first.', color: 'error' },
    postinterest: { title: 'Post Interest', message: `Post all accrued interest up to ${today}?`, color: 'primary' },
  };

  return (
    <Box>
      <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/accounts')} sx={{ mb: 2 }} variant="text" color="inherit">
        Back to Accounts
      </Button>

      <PageHeader
        title={isLoading ? '…' : (account?.accountNo ?? 'Account')}
        subtitle="Savings account"
        actions={
          <Box display="flex" flexWrap="wrap" gap={1}>
            {account?.status === 'Submitted' && (
              <RoleGuard roles={CAN.approveAccount}>
                <Button variant="contained" color="success" startIcon={<CheckIcon />} size="small" onClick={() => setConfirm('approve')}>Approve</Button>
                <Button variant="outlined" color="error" startIcon={<BlockIcon />} size="small" onClick={() => setConfirm('reject')}>Reject</Button>
                <Button variant="outlined" startIcon={<CloseIcon />} size="small" onClick={() => setConfirm('withdraw')}>Withdraw</Button>
              </RoleGuard>
            )}
            {account?.status === 'Approved' && (
              <RoleGuard roles={CAN.activateAccount}>
                <Button variant="contained" startIcon={<PlayArrowIcon />} size="small" onClick={() => setConfirm('activate')}>Activate</Button>
              </RoleGuard>
            )}
            {account?.status === 'Active' && (
              <>
                <RoleGuard roles={CAN.deposit}>
                  <Button variant="contained" color="success" startIcon={<SavingsIcon />} size="small" onClick={() => setTxDialog('deposit')}>Deposit</Button>
                  <Button variant="outlined" color="warning" startIcon={<SavingsIcon />} size="small" onClick={() => setTxDialog('withdraw')}>Withdraw</Button>
                </RoleGuard>
                <RoleGuard roles={CAN.postInterest}>
                  <Button variant="outlined" startIcon={<TrendingUpIcon />} size="small" onClick={() => setConfirm('postinterest')}>Post Interest</Button>
                </RoleGuard>
                <RoleGuard roles={CAN.closeAccount}>
                  <Button variant="outlined" color="error" startIcon={<CloseIcon />} size="small" onClick={() => setConfirm('close')}>Close</Button>
                </RoleGuard>
              </>
            )}
          </Box>
        }
      />

      {isError && <Alert severity="error" sx={{ mb: 2 }}>Failed to load account.</Alert>}

      {/* Balance hero */}
      {account && (
        <Card
          sx={{
            mb: 3,
            color: 'common.white',
            border: 'none',
            background: (t) =>
              `linear-gradient(120deg, ${t.palette.primary.dark} 0%, ${t.palette.primary.main} 60%, ${t.palette.secondary.main} 130%)`,
          }}
        >
          <CardContent sx={{ p: { xs: 2.5, md: 3.5 } }}>
            <Typography sx={{ opacity: 0.8, fontSize: '0.8rem', fontWeight: 600, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
              Current Balance
            </Typography>
            <Typography
              sx={{
                fontFamily: '"Plus Jakarta Sans", sans-serif',
                fontWeight: 800,
                fontSize: { xs: '2.2rem', md: '2.8rem' },
                lineHeight: 1.1,
                mt: 0.5,
                ...tabularNums,
              }}
            >
              {account.currencyCode}{' '}
              {account.accountBalance.toLocaleString(undefined, { minimumFractionDigits: 2 })}
            </Typography>
            <Box display="flex" flexWrap="wrap" gap={3} mt={2.5} sx={{ opacity: 0.92 }}>
              <Box>
                <Typography sx={{ fontSize: '0.72rem', opacity: 0.75, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                  Interest Rate
                </Typography>
                <Typography fontWeight={700}>{account.nominalAnnualRate}% p.a.</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '0.72rem', opacity: 0.75, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                  Status
                </Typography>
                <Typography fontWeight={700}>{account.status}</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '0.72rem', opacity: 0.75, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                  Interest Posted Till
                </Typography>
                <Typography fontWeight={700}>{account.interestPostedTillDate ?? '—'}</Typography>
              </Box>
            </Box>
          </CardContent>
        </Card>
      )}

      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" mb={2}>Account Details</Typography>
              {isLoading ? (
                Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} height={40} />)
              ) : (
                <>
                  <InfoRow label="Account No." value={account?.accountNo} />
                  <InfoRow
                    label="Client"
                    value={
                      account ? (
                        <Link
                          component="button"
                          type="button"
                          onClick={() => navigate(`/clients/${account.clientId}`)}
                          sx={{ fontWeight: 600 }}
                        >
                          {client?.displayName ?? 'View client'}
                        </Link>
                      ) : null
                    }
                  />
                  <InfoRow
                    label="Product"
                    value={
                      account ? (
                        <Link
                          component="button"
                          type="button"
                          onClick={() => navigate(`/products/${account.productId}`)}
                          sx={{ fontWeight: 600 }}
                        >
                          {product?.name ?? 'View product'}
                        </Link>
                      ) : null
                    }
                  />
                  <InfoRow label="Status" value={account ? <StatusChip status={account.status} /> : null} />
                  <InfoRow label="Currency" value={account?.currencyCode} />
                  <InfoRow label="Interest Rate" value={`${account?.nominalAnnualRate}% p.a.`} />
                  <InfoRow label="Balance" value={account?.accountBalance.toLocaleString(undefined, { minimumFractionDigits: 2 })} />
                  <InfoRow label="Submitted On" value={account?.submittedOn} />
                  <InfoRow label="Approved On" value={account?.approvedOn} />
                  <InfoRow label="Activated On" value={account?.activatedOn} />
                  <InfoRow label="Interest Posted Till" value={account?.interestPostedTillDate} />
                  {account?.closedOn && <InfoRow label="Closed On" value={account.closedOn} />}
                </>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Transactions */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" mb={2}>Transaction History</Typography>
              <Divider sx={{ mb: 1 }} />
              {(transactions ?? []).length === 0 ? (
                <Typography color="text.secondary" variant="body2">No transactions yet.</Typography>
              ) : (
                [...(transactions ?? [])].reverse().map((tx) => (
                  <Box key={tx.id} display="flex" justifyContent="space-between" py={1.25} borderBottom="1px solid" borderColor="divider">
                    <Box>
                      <Typography variant="body2" fontWeight={600}>
                        {TRANSACTION_TYPE_LABELS[tx.typeId] ?? tx.type}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">{tx.transactionDate}</Typography>
                    </Box>
                    <Box textAlign="right">
                      <Typography
                        variant="body2"
                        fontWeight={600}
                        color={tx.typeId === 2 ? 'error.main' : tx.typeId === 3 ? 'success.main' : 'text.primary'}
                      >
                        {tx.typeId === 2 ? '−' : '+'}{tx.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Bal: {tx.runningBalance.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                      </Typography>
                    </Box>
                  </Box>
                ))
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Confirm dialog */}
      {confirm && CONFIRM_META[confirm] && (
        <ConfirmDialog
          open
          title={CONFIRM_META[confirm].title}
          message={CONFIRM_META[confirm].message}
          confirmColor={CONFIRM_META[confirm].color}
          loading={activeMutation.isPending}
          onConfirm={handleConfirm}
          onCancel={() => setConfirm(null)}
        />
      )}

      {/* Transaction dialog */}
      {txDialog && (
        <TransactionDialog
          open
          type={txDialog}
          accountId={id!}
          onClose={() => setTxDialog(null)}
        />
      )}
    </Box>
  );
}
