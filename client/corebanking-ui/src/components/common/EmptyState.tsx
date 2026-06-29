import { Box, Typography, alpha } from '@mui/material';
import type { ReactNode } from 'react';

interface Props {
  icon: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
}

/** Friendly empty/zero-result state with a tinted icon medallion. */
export function EmptyState({ icon, title, description, action }: Props) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        textAlign: 'center',
        py: 7,
        px: 3,
      }}
    >
      <Box
        sx={{
          width: 64,
          height: 64,
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: 'primary.main',
          mb: 2,
          backgroundColor: (t) => alpha(t.palette.primary.main, t.palette.mode === 'dark' ? 0.16 : 0.1),
          '& svg': { fontSize: 30 },
        }}
      >
        {icon}
      </Box>
      <Typography variant="subtitle1">{title}</Typography>
      {description && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, maxWidth: 360 }}>
          {description}
        </Typography>
      )}
      {action && <Box mt={2.5}>{action}</Box>}
    </Box>
  );
}
