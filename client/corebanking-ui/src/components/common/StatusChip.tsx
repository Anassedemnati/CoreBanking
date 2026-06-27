import { Chip, type ChipProps } from '@mui/material';

const ACCOUNT_STATUS_COLORS: Record<
  string,
  { color: ChipProps['color']; label: string }
> = {
  Submitted: { color: 'info', label: 'Submitted' },
  Approved: { color: 'warning', label: 'Approved' },
  Active: { color: 'success', label: 'Active' },
  Withdrawn: { color: 'default', label: 'Withdrawn' },
  Rejected: { color: 'error', label: 'Rejected' },
  Closed: { color: 'default', label: 'Closed' },
};

const CLIENT_STATUS_COLORS: Record<string, { color: ChipProps['color']; label: string }> = {
  Pending: { color: 'warning', label: 'Pending' },
  Active: { color: 'success', label: 'Active' },
};

interface Props {
  status: string;
  variant?: 'account' | 'client';
  size?: ChipProps['size'];
}

export function StatusChip({ status, variant = 'account', size = 'small' }: Props) {
  const map = variant === 'client' ? CLIENT_STATUS_COLORS : ACCOUNT_STATUS_COLORS;
  const config = map[status] ?? { color: 'default' as const, label: status };
  return (
    <Chip
      label={config.label}
      color={config.color}
      size={size}
      variant="filled"
      sx={{ fontWeight: 600 }}
    />
  );
}
