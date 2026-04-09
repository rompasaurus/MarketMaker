import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useSignalR } from '@/hooks/useSignalR';
import { AppShell } from '@/components/layout/AppShell';
import { DashboardPage } from '@/pages/DashboardPage';
import { MarketPage } from '@/pages/MarketPage';
import { SettingsPage } from '@/pages/SettingsPage';
import { MarketList } from '@/components/markets/MarketList';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 10_000,
      retry: 2,
    },
  },
});

function AppContent() {
  const { connection, connectionState } = useSignalR();

  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppShell connectionState={connectionState} />}>
          <Route index element={<DashboardPage connection={connection} />} />
          <Route path="markets" element={<MarketList />} />
          <Route path="markets/:id" element={<MarketPage connection={connection} />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AppContent />
    </QueryClientProvider>
  );
}
