import { Search, Wifi, WifiOff, Menu } from 'lucide-react';
import { HubConnectionState } from '@microsoft/signalr';
import { useAppStore } from '@/stores/useAppStore';

interface HeaderProps {
  connectionState: HubConnectionState;
}

export function Header({ connectionState }: HeaderProps) {
  const { searchQuery, setSearchQuery, toggleSidebar } = useAppStore();
  const isConnected = connectionState === HubConnectionState.Connected;

  return (
    <header className="flex items-center gap-4 border-b border-gray-800 bg-gray-900 px-4 py-3">
      <button onClick={toggleSidebar} className="text-gray-400 hover:text-white">
        <Menu size={20} />
      </button>

      <h1 className="text-lg font-bold text-white">Polymarket Tracker</h1>

      <div className="relative ml-4 flex-1 max-w-md">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" />
        <input
          type="text"
          placeholder="Search markets..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full rounded-lg bg-gray-800 py-2 pl-10 pr-4 text-sm text-gray-200 placeholder-gray-500 outline-none focus:ring-1 focus:ring-indigo-500"
        />
      </div>

      <div className="ml-auto flex items-center gap-2 text-sm">
        {isConnected ? (
          <span className="flex items-center gap-1 text-green-400">
            <Wifi size={14} /> Live
          </span>
        ) : (
          <span className="flex items-center gap-1 text-red-400">
            <WifiOff size={14} /> Disconnected
          </span>
        )}
      </div>
    </header>
  );
}
