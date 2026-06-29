import { Box, useTheme } from '@mui/material';
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from 'recharts';

export interface TrendPoint {
  label: string;
  count: number;
}

/** Area chart of accounts opened per period (real data from submittedOn). */
export function AccountsTrendChart({ data }: { data: TrendPoint[] }) {
  const theme = useTheme();
  const accent = theme.palette.primary.main;

  return (
    <Box sx={{ width: '100%', height: 260 }}>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
          <defs>
            <linearGradient id="cbTrend" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={accent} stopOpacity={0.32} />
              <stop offset="100%" stopColor={accent} stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid stroke={theme.palette.divider} vertical={false} />
          <XAxis
            dataKey="label"
            tick={{ fill: theme.palette.text.secondary, fontSize: 12 }}
            axisLine={false}
            tickLine={false}
          />
          <YAxis
            allowDecimals={false}
            tick={{ fill: theme.palette.text.secondary, fontSize: 12 }}
            axisLine={false}
            tickLine={false}
            width={36}
          />
          <Tooltip
            cursor={{ stroke: accent, strokeWidth: 1, strokeDasharray: '4 4' }}
            contentStyle={{
              background: theme.palette.background.paper,
              border: `1px solid ${theme.palette.divider}`,
              borderRadius: 12,
              boxShadow: theme.shadows[3],
              color: theme.palette.text.primary,
            }}
            labelStyle={{ color: theme.palette.text.secondary, fontWeight: 600 }}
          />
          <Area
            type="monotone"
            dataKey="count"
            name="Accounts opened"
            isAnimationActive={false}
            stroke={accent}
            strokeWidth={2.5}
            fill="url(#cbTrend)"
            activeDot={{ r: 5, strokeWidth: 2, stroke: theme.palette.background.paper }}
            dot={{ r: 3, fill: accent, strokeWidth: 0 }}
          />
        </AreaChart>
      </ResponsiveContainer>
    </Box>
  );
}
