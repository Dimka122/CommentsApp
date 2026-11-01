import { useEffect, useState } from 'react';
import { searchComments } from '../api';
import type { SearchPage } from '../types';
import { formatDate } from './CommentParts';
import { Pagination } from './Pagination';
import { SafeHtml } from './SafeHtml';

export function SearchPanel({ query }: { query: string }) {
  const [page, setPage] = useState(1);
  const [data, setData] = useState<SearchPage | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    searchComments(query, page)
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch((e: unknown) => { if (!cancelled) setError(e instanceof Error ? e.message : 'Search failed'); });
    return () => { cancelled = true; };
  }, [query, page]);

  return (
    <section className="card search-results">
      <h2>Search: “{query}” {data && <span className="count">{data.totalCount}</span>}</h2>
      {error && <p className="err">{error}</p>}
      {data && data.items.length === 0 && <p className="muted">Nothing found.</p>}
      <ul>
        {data?.items.map((h) => (
          <li key={h.id}>
            <div className="meta"><strong>{h.userName}</strong> <time className="muted">{formatDate(h.createdAt)}</time>
              {h.parentId && <span className="badge">reply</span>}</div>
            <SafeHtml html={h.snippet} className="text" />
          </li>
        ))}
      </ul>
      {data && <Pagination page={page} totalPages={Math.ceil(data.totalCount / data.pageSize)} onChange={setPage} />}
    </section>
  );
}
