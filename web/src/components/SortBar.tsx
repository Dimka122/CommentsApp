import type { SortBy } from '../types';

const ITEMS: [SortBy, string][] = [['CREATED_AT', 'Date'], ['USER_NAME', 'Name'], ['EMAIL', 'E-mail']];

export function SortBar({ sortBy, desc, onSort }: { sortBy: SortBy; desc: boolean; onSort: (c: SortBy) => void }) {
  return (
    <div className="sortbar" role="group" aria-label="Sort comments">
      <span className="muted">Sort by</span>
      {ITEMS.map(([col, label]) => (
        <button key={col} type="button" className={'chip' + (sortBy === col ? ' active' : '')}
          aria-pressed={sortBy === col} onClick={() => onSort(col)}>
          {label}{sortBy === col ? (desc ? ' ▼' : ' ▲') : ''}
        </button>
      ))}
    </div>
  );
}
