interface Props { page: number; totalPages: number; onChange: (p: number) => void }

export function Pagination({ page, totalPages, onChange }: Props) {
  if (totalPages <= 1) return null;
  const pages = new Set<number>([1, totalPages]);
  for (let p = page - 2; p <= page + 2; p++) if (p >= 1 && p <= totalPages) pages.add(p);
  const list = [...pages].sort((a, b) => a - b);

  return (
    <nav className="pagination" aria-label="Pages">
      <button disabled={page <= 1} onClick={() => onChange(page - 1)}>‹ Prev</button>
      {list.map((p, i) => (
        <span key={p}>
          {i > 0 && p - list[i - 1] > 1 && <span className="gap">…</span>}
          <button className={p === page ? 'active' : ''} aria-current={p === page ? 'page' : undefined} onClick={() => onChange(p)}>{p}</button>
        </span>
      ))}
      <button disabled={page >= totalPages} onClick={() => onChange(page + 1)}>Next ›</button>
    </nav>
  );
}
