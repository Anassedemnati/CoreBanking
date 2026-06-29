import { Chip, alpha, type ChipProps } from '@mui/material';

type StatusColor = 'success' | 'error' | 'warning' | 'info' | 'default';

const ACCOUNT_STATUS: Record<string, { color: StatusColor; label: string }> = {
  Submitted: { color: 'info', label: 'Submitted' },
  Approved: { color: 'warning', label: 'Approved' },
  Active: { color: 'success', label: 'Active' },
  Withdrawn: { color: 'default', label: 'Withdrawn' },
  Rejected: { color: 'error', label: 'Rejected' },
  Closed: { color: 'default', label: 'Closed' },
};

const CLIENT_STATUS: Record<string, { color: StatusColor; label: string }> = {
  Pending: { color: 'warning', label: 'Pending' },
  Active: { color: 'success', label: 'Active' },
};

interface Props {
  status: string;
  variant?: 'account' | 'client';
  size?: ChipProps['size'];
}

/**
 * Soft, tinted status pill with a leading dot — readable in light and dark.
 */
export function StatusChip({ status, variant = 'account', size = 'small' }: Props) {
  const map = variant === 'client' ? CLIENT_STATUS : ACCOUNT_STATUS;
  const { color, label } = map[status] ?? { color: 'default' as const, label: status };

  return (
    <Chip
      label={label}
      size={size}
      sx={(t) => {
        const c = color === 'default' ? t.palette.text.secondary : t.palette[color].main;
        return {
          fontWeight: 600,
          color: c,
          backgroundColor: alpha(c, t.palette.mode === 'dark' ? 0.16 : 0.12),
          border: `1px solid ${alpha(c, 0.22)}`,
          '& .MuiChip-label': { display: 'flex', alignItems: 'center', gap: 0.75 },
          '&::before': {
            content: '""',
            display: 'inline-block',
            width: 6,
            height: 6,
            borderRadius: '50%',
            backgroundColor: c,
            marginLeft: '8px',
          },
        };
      }}
    />
  );
}
