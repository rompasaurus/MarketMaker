import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { SIGNALR_URL } from './constants';

export function createHubConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(SIGNALR_URL)
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();
}
