import { Outlet } from 'react-router-dom';
import { HubConnectionState } from '@microsoft/signalr';
import { Header } from './Header';
import { Sidebar } from './Sidebar';

interface AppShellProps {
  connectionState: HubConnectionState;
}

export function AppShell({ connectionState }: AppShellProps) {
  return (
    <div className="flex h-screen flex-col">
      <Header connectionState={connectionState} />
      <div className="flex flex-1 overflow-hidden">
        <Sidebar />
        <main className="flex-1 overflow-y-auto bg-gray-950 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
