import { useEffect, useRef, useState } from 'react';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { createHubConnection } from '@/lib/signalr';

export function useSignalR() {
  const connectionRef = useRef<HubConnection | null>(null);
  const [state, setState] = useState<HubConnectionState>(HubConnectionState.Disconnected);

  useEffect(() => {
    const connection = createHubConnection();
    connectionRef.current = connection;

    connection.onreconnecting(() => setState(HubConnectionState.Reconnecting));
    connection.onreconnected(() => setState(HubConnectionState.Connected));
    connection.onclose(() => setState(HubConnectionState.Disconnected));

    connection.start()
      .then(() => setState(HubConnectionState.Connected))
      .catch((err) => console.error('SignalR connection failed:', err));

    return () => {
      connection.stop();
    };
  }, []);

  return { connection: connectionRef.current, connectionState: state };
}
