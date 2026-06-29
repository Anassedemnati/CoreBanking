import { Card, CardContent, Box, Typography, Chip, Skeleton } from '@mui/material';
import AccountBalanceRoundedIcon from '@mui/icons-material/AccountBalanceRounded';
import ChevronRightRoundedIcon from '@mui/icons-material/ChevronRightRounded';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { StatusChip } from './StatusChip';
import { EmptyState } from './EmptyState';
import { tabularNums } from '../../theme/theme';
import type { SavingsAccountDto } from '../../api/types';

interface Props {
  title: string;
  accounts: SavingsAccountDto[];
  isLoading?: boolean;
  emptyText?: string;
  action?: ReactNode;
}

/**
 * Lists the savings accounts related to a client or product (the "1 ─< N" side
 * of the relationship). Each row links to the account detail page.
 */
export function RelatedAccountsCard({ title, accounts, isLoading, emptyText, action }: Props) {
  const navigate = useNavigate();

  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between" mb={1.5}>
          <Box display="flex" alignItems="center" gap={1}>
            <Typography variant="subtitle1">{title}</Typography>
            {!isLoading && (
              <Chip label={accounts.length} size="small" sx={{ height: 20, fontWeight: 700 }} />
            )}
          </Box>
          {action}
        </Box>

        {isLoading ? (
          Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} height={52} />)
        ) : accounts.length === 0 ? (
          <EmptyState
            icon={<AccountBalanceRoundedIcon />}
            title="No linked accounts"
            description={emptyText}
          />
        ) : (
          accounts.map((a) => (
            <Box
              key={a.id}
              onClick={() => navigate(`/accounts/${a.id}`)}
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1.5,
                py: 1.25,
                px: 1,
                mx: -1,
                borderRadius: 2,
                cursor: 'pointer',
                '&:hover': { backgroundColor: 'action.hover' },
              }}
            >
              <Box flexGrow={1} minWidth={0}>
                <Typography variant="body2" fontWeight={700} noWrap>
                  {a.accountNo}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {a.currencyCode} · {a.nominalAnnualRate}% p.a.
                </Typography>
              </Box>
              <Typography variant="body2" fontWeight={700} sx={tabularNums}>
                {a.accountBalance.toLocaleString(undefined, { minimumFractionDigits: 2 })}
              </Typography>
              <StatusChip status={a.status} size="small" />
              <ChevronRightRoundedIcon fontSize="small" sx={{ color: 'text.disabled' }} />
            </Box>
          ))
        )}
      </CardContent>
    </Card>
  );
}
