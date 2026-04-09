import { useEffect, useRef } from 'react';
import { createChart, type IChartApi, type ISeriesApi, type LineSeries } from 'lightweight-charts';
import type { PriceHistoryPoint } from '@/api/markets';

interface OddsChartProps {
  history: PriceHistoryPoint[];
  latestPrice?: number | null;
}

export function OddsChart({ history, latestPrice }: OddsChartProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<'Line'> | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    const chart = createChart(containerRef.current, {
      layout: {
        background: { color: '#111827' },
        textColor: '#9ca3af',
      },
      grid: {
        vertLines: { color: '#1f2937' },
        horzLines: { color: '#1f2937' },
      },
      width: containerRef.current.clientWidth,
      height: 400,
      rightPriceScale: {
        borderColor: '#374151',
      },
      timeScale: {
        borderColor: '#374151',
        timeVisible: true,
      },
      crosshair: {
        horzLine: { color: '#6366f1' },
        vertLine: { color: '#6366f1' },
      },
    });

    const series = chart.addSeries('Line', {
      color: '#6366f1',
      lineWidth: 2,
      priceFormat: {
        type: 'custom',
        formatter: (price: number) => `${(price * 100).toFixed(1)}%`,
      },
    });

    chartRef.current = chart;
    seriesRef.current = series;

    const handleResize = () => {
      if (containerRef.current) {
        chart.applyOptions({ width: containerRef.current.clientWidth });
      }
    };
    window.addEventListener('resize', handleResize);

    return () => {
      window.removeEventListener('resize', handleResize);
      chart.remove();
    };
  }, []);

  // Update data when history changes
  useEffect(() => {
    if (!seriesRef.current || !history.length) return;

    const data = history.map((p) => ({
      time: Math.floor(new Date(p.timestamp).getTime() / 1000) as any,
      value: p.price,
    }));

    seriesRef.current.setData(data);
    chartRef.current?.timeScale().fitContent();
  }, [history]);

  // Append real-time price updates
  useEffect(() => {
    if (!seriesRef.current || latestPrice === null || latestPrice === undefined) return;

    seriesRef.current.update({
      time: Math.floor(Date.now() / 1000) as any,
      value: latestPrice,
    });
  }, [latestPrice]);

  return (
    <div
      ref={containerRef}
      className="rounded-xl border border-gray-800 bg-gray-900 overflow-hidden"
    />
  );
}
