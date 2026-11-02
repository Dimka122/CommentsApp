import type { SortBy } from '../types';
import type { View } from '../useView';

const ITEMS: [SortBy, string][] = [['CREATED_AT', 'Date'], ['USER_NAME', 'Name'], ['EMAIL', 'E-mail']];

interface Props {
  sortBy: SortBy;
  desc: boolean;
  onSort: (c: SortBy) => void;
  view: View;
  onView: (v: View) => void;
}

export function SortBar({ sortBy, desc, onSort, view, onView }: Props) {
  return (
    <div className="sortbar">
      <div className="sort-group" role="group" aria-label="Sort comments">
        <span className="muted">Sort by</span>
        {ITEMS.map(([col, label]) => (
          <button key={col} type="button" className={'chip' + (sortBy === col ? ' active' : '')}
            aria-pressed={sortBy === col} onClick={() => onSort(col)}>
            {label}{sortBy === col ? (desc ? ' ▼' : ' ▲') : ''}
          </button>
        ))}
      </div>
      <div className="view-toggle" role="group" aria-label="View">
        <button type="button" className={'chip' + (view === 'cards' ? ' active' : '')} aria-pressed={view === 'cards'}
          onClick={() => onView('cards')}>Cards</button>
        <button type="button" className={'chip' + (view === 'table' ? ' active' : '')} aria-pressed={view === 'table'}
          onClick={() => onView('table')}>Table</button>
      </div>
    </div>
  );
}