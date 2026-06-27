import { Box, Typography, Button } from '@mui/material';
import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';

export default function UnauthorizedPage() {
  const navigate = useNavigate();
  const { logout } = useAuth();

  return (
    <Box
      display="flex"
      flexDirection="column"
      alignItems="center"
      justifyContent="center"
      minHeight="80vh"
      gap={2}
      textAlign="center"
      px={3}
    >
      <LockOutlinedIcon sx={{ fontSize: 64, color: 'text.disabled' }} />
      <Typography variant="h5" fontWeight={700}>
        Access Denied
      </Typography>
      <Typography color="text.secondary" maxWidth={400}>
        You don&apos;t have permission to view this page. Contact your supervisor if you
        believe this is an error.
      </Typography>
      <Box display="flex" gap={2} mt={1}>
        <Button variant="outlined" onClick={() => navigate('/')}>
          Go to Dashboard
        </Button>
        <Button variant="text" color="error" onClick={logout}>
          Sign Out
        </Button>
      </Box>
    </Box>
  );
}
