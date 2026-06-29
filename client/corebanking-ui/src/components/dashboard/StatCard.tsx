import { Card, CardContent, Box, Typography, Skeleton, alpha } from '@mui/material';
import type { ReactNode } from 'react';
import { tabularNums } from '../../theme/theme';

interface Props {
  icon: ReactNode;
  label: string;
  value: ReactNode;
  hint?: string;
  /** Theme palette key driving the icon tint. */
  tone?: 'primary' | 'success' | 'warning' | 'info';
  loading?: boolean;
}

export function StatCard({ icon, label, value, hint, tone = 'primary', loading }: Props) {
  return (
    <Card sx={{ height: '100%', overflow: 'hidden', position: 'relative' }}>
      <CardContent sx={{ p: 2.5 }}>
        <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between' }}>
          <Box
            sx={{
              width: 44,
              height: 44,
              borderRadius: 3,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: `${tone}.main`,
              backgroundColor: (t) => alpha(t.palette[tone].main, t.palette.mode === 'dark' ? 0.18 : 0.1),
            }}
          >
            {icon}
          </Box>
        </Box>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2, fontWeight: 600 }}>
          {label}
        </Typography>
        {loading ? (
          <Skeleton width={72} height={40} />
        ) : (
          <Typography
            sx={{
              fontFamily: '"Plus Jakarta Sans", sans-serif',
              fontWeight: 800,
              fontSize: '1.9rem',
              lineHeight: 1.1,
              letterSpacing: '-0.02em',
              mt: 0.25,
              ...tabularNums,
            }}
          >
            {value ?? '—'}
          </Typography>
        )}
        {hint && (
          <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
            {hint}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}
