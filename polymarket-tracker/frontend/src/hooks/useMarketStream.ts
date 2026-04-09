import { useEffect, useRef, useState, useCallback } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import type { Trade } from '@/types/trade';

const MAX_TRADES = 100;

export function useMarketStream(connection: HubConnection | null, conditionId?: string) {
  const [trades, setTrades] = useState<Trade[]>([]);
  const [latestPrice, setLatestPrice] = useState<number | null>(null);
  const joinedRef = useRef<string | null>(null);

  const addTrade = useCallback((trade: Trade) => {
    setTrades((prev) => [trade, ...prev].slice(0, MAX_TRADES));
    setLatestPrice(trade.price);
  }, []);

  useEffect(() => {
    if (!connection) return;

    if (conditionId) {
      // Subscribe to specific market
      connection.invoke('JoinMarket', conditionId).catch(console.error);
      joinedRef.current = conditionId;

      connection.on('ReceiveTrade', addTrade);
    } else {
      // Subscribe to global trade feed
      connection.on('ReceiveGlobalTrade', addTrade);
    }

    return () => {
      if (conditionId && joinedRef.current) {
        connection.invoke('LeaveMarket', joinedRef.current).catch(console.error);
        joinedRef.current = null;
      }
      connection.off('ReceiveTrade', addTrade);
      connection.off('ReceiveGlobalTrade', addTrade);
    };
  }, [connection, conditionId, addTrade]);

  return { trades, latestPrice };
}
