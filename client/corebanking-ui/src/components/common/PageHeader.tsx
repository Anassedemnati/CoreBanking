import { Box, Typography, type SxProps } from '@mui/material';
import type { ReactNode } from 'react';

interface Props {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  sx?: SxProps;
}

export function PageHeader({ title, subtitle, actions, sx }: Props) {
  return (
    <Box
      display="flex"
      alignItems={{ xs: 'flex-start', sm: 'center' }}
      flexDirection={{ xs: 'column', sm: 'row' }}
      justifyContent="space-between"
      gap={2}
      mb={3}
      sx={sx}
    >
      <Box>
        <Typography variant="h5" component="h1">
          {title}
        </Typography>
        {subtitle && (
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            {subtitle}
          </Typography>
        )}
      </Box>
      {actions && <Box flexShrink={0}>{actions}</Box>}
    </Box>
  );
}
