import { Box, Typography, Button, Card, CardContent, alpha } from '@mui/material';
import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';
import { BrandLogo } from '../branding/BrandLogo';

export default function UnauthorizedPage() {
  const navigate = useNavigate();
  const { logout } = useAuth();

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 4,
        px: 3,
      }}
    >
      <BrandLogo variant="full" size={40} />

      <Card sx={{ maxWidth: 440, width: '100%' }}>
        <CardContent sx={{ p: 4, textAlign: 'center' }}>
          <Box
            sx={{
              width: 72,
              height: 72,
              mx: 'auto',
              mb: 2.5,
              borderRadius: '50%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'error.main',
              backgroundColor: (t) => alpha(t.palette.error.main, t.palette.mode === 'dark' ? 0.16 : 0.1),
            }}
          >
            <LockOutlinedIcon sx={{ fontSize: 34 }} />
          </Box>
          <Typography variant="h5" mb={1}>
            Access Denied
          </Typography>
          <Typography color="text.secondary" mb={3}>
            You don&apos;t have permission to view this page. Contact your supervisor if you
            believe this is an error.
          </Typography>
          <Box display="flex" gap={1.5} justifyContent="center">
            <Button variant="contained" onClick={() => navigate('/')}>
              Go to Dashboard
            </Button>
            <Button variant="text" color="error" onClick={logout}>
              Sign Out
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
