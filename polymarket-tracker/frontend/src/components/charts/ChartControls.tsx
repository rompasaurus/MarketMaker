import { TIMEFRAMES, type Timeframe } from '@/lib/constants';

interface ChartControlsProps {
  selected: Timeframe;
  onSelect: (tf: Timeframe) => void;
}

export function ChartControls({ selected, onSelect }: ChartControlsProps) {
  return (
    <div className="flex gap-1">
      {TIMEFRAMES.map(({ label, value }) => (
        <button
          key={value}
          onClick={() => onSelect(value)}
          className={`rounded-lg px-3 py-1.5 text-xs font-medium transition-colors ${
            selected === value
              ? 'bg-indigo-600 text-white'
              : 'bg-gray-800 text-gray-400 hover:bg-gray-700 hover:text-white'
          }`}
        >
          {label}
        </button>
      ))}
    </div>
  );
}
