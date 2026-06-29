import { Box, Stack, Typography, useTheme } from '@mui/material';
import { ResponsiveContainer, PieChart, Pie, Cell, Tooltip } from 'recharts';

export interface DonutSlice {
  name: string;
  value: number;
  color: string;
}

/** Donut of account-status mix with a centered total and a legend. */
export function StatusDonut({ data, total }: { data: DonutSlice[]; total: number }) {
  const theme = useTheme();
  const slices = data.filter((d) => d.value > 0);

  return (
    <Stack direction={{ xs: 'column', sm: 'row' }} alignItems="center" spacing={2}>
      <Box sx={{ position: 'relative', width: 180, height: 180, flexShrink: 0 }}>
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={slices.length ? slices : [{ name: 'None', value: 1, color: theme.palette.divider }]}
              dataKey="value"
              isAnimationActive={false}
              innerRadius={58}
              outerRadius={84}
              paddingAngle={slices.length > 1 ? 3 : 0}
              startAngle={90}
              endAngle={-270}
              stroke="none"
            >
              {(slices.length ? slices : [{ name: 'None', value: 1, color: theme.palette.divider }]).map(
                (s) => (
                  <Cell key={s.name} fill={s.color} />
                ),
              )}
            </Pie>
            {slices.length > 0 && (
              <Tooltip
                contentStyle={{
                  background: theme.palette.background.paper,
                  border: `1px solid ${theme.palette.divider}`,
                  borderRadius: 12,
                  color: theme.palette.text.primary,
                }}
              />
            )}
          </PieChart>
        </ResponsiveContainer>
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            pointerEvents: 'none',
          }}
        >
          <Typography sx={{ fontFamily: '"Plus Jakarta Sans"', fontWeight: 800, fontSize: '1.6rem', lineHeight: 1 }}>
            {total}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            accounts
          </Typography>
        </Box>
      </Box>

      <Stack spacing={1} sx={{ width: '100%' }}>
        {slices.length === 0 && (
          <Typography variant="body2" color="text.secondary">
            No accounts yet.
          </Typography>
        )}
        {slices.map((s) => (
          <Stack key={s.name} direction="row" alignItems="center" spacing={1.25}>
            <Box sx={{ width: 10, height: 10, borderRadius: '50%', backgroundColor: s.color }} />
            <Typography variant="body2" sx={{ flexGrow: 1 }}>
              {s.name}
            </Typography>
            <Typography variant="body2" fontWeight={700}>
              {s.value}
            </Typography>
          </Stack>
        ))}
      </Stack>
    </Stack>
  );
}
