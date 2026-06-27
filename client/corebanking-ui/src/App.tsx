import { ThemeProvider, CssBaseline } from '@mui/material';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { theme } from './theme/theme';
import { AuthProvider } from './auth/AuthProvider';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { AppShell } from './components/layout/AppShell';
import { CAN, ROLES } from './auth/roles';

import DashboardPage from './pages/DashboardPage';
import UnauthorizedPage from './pages/UnauthorizedPage';
import ClientsPage from './pages/clients/ClientsPage';
import ClientDetailPage from './pages/clients/ClientDetailPage';
import NewClientPage from './pages/clients/NewClientPage';
import ProductsPage from './pages/products/ProductsPage';
import ProductDetailPage from './pages/products/ProductDetailPage';
import NewProductPage from './pages/products/NewProductPage';
import AccountsPage from './pages/accounts/AccountsPage';
import AccountDetailPage from './pages/accounts/AccountDetailPage';
import NewAccountWizard from './pages/accounts/NewAccountWizard';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
  },
});

const ALL_ROLES = Object.values(ROLES);

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <AuthProvider>
          <BrowserRouter>
            <Routes>
              <Route path="/unauthorized" element={<UnauthorizedPage />} />
              <Route
                element={
                  <ProtectedRoute roles={ALL_ROLES}>
                    <AppShell />
                  </ProtectedRoute>
                }
              >
                <Route index element={<DashboardPage />} />

                {/* Clients */}
                <Route path="/clients" element={<ProtectedRoute roles={ALL_ROLES}><ClientsPage /></ProtectedRoute>} />
                <Route path="/clients/new" element={<ProtectedRoute roles={CAN.registerClient}><NewClientPage /></ProtectedRoute>} />
                <Route path="/clients/:id" element={<ProtectedRoute roles={ALL_ROLES}><ClientDetailPage /></ProtectedRoute>} />

                {/* Products */}
                <Route path="/products" element={<ProtectedRoute roles={ALL_ROLES}><ProductsPage /></ProtectedRoute>} />
                <Route path="/products/new" element={<ProtectedRoute roles={CAN.createProduct}><NewProductPage /></ProtectedRoute>} />
                <Route path="/products/:id" element={<ProtectedRoute roles={ALL_ROLES}><ProductDetailPage /></ProtectedRoute>} />

                {/* Accounts */}
                <Route path="/accounts" element={<ProtectedRoute roles={ALL_ROLES}><AccountsPage /></ProtectedRoute>} />
                <Route path="/accounts/new" element={<ProtectedRoute roles={CAN.submitAccount}><NewAccountWizard /></ProtectedRoute>} />
                <Route path="/accounts/:id" element={<ProtectedRoute roles={ALL_ROLES}><AccountDetailPage /></ProtectedRoute>} />

                <Route path="*" element={<Navigate to="/" replace />} />
              </Route>
            </Routes>
          </BrowserRouter>
        </AuthProvider>
      </ThemeProvider>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
