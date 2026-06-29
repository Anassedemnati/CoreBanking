import { IconButton, Tooltip } from '@mui/material';
import LightModeRoundedIcon from '@mui/icons-material/LightModeRounded';
import DarkModeRoundedIcon from '@mui/icons-material/DarkModeRounded';
import { useColorMode } from '../../theme/ColorModeProvider';

export function ColorModeToggle() {
  const { mode, toggle } = useColorMode();
  const isDark = mode === 'dark';
  return (
    <Tooltip title={isDark ? 'Switch to light mode' : 'Switch to dark mode'}>
      <IconButton onClick={toggle} size="small" sx={{ color: 'text.secondary' }}>
        {isDark ? <LightModeRoundedIcon fontSize="small" /> : <DarkModeRoundedIcon fontSize="small" />}
      </IconButton>
    </Tooltip>
  );
}
