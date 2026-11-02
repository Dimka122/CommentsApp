import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { fetchCommentsPage } from './api';
import { CommentForm } from './components/CommentForm';
import { CommentCards } from './components/CommentCards';
import { CommentTable } from './components/CommentTable';
import { ComposeDialog } from './components/ComposeDialog';
import { Lightbox } from './components/Lightbox';
import type { LightboxItem } from './components/Lightbox';
import { Pagination } from './components/Pagination';
import { SearchPanel } from './components/SearchPanel';
import { SortBar } from './components/SortBar';
import { buildNode } from './tree';
import type { ApiComment, CommentPage, SortBy } from './types';
import { useCommentsHub } from './useCommentsHub';
import { MeContext } from './AuthorContext';
import type { Me } from './AuthorContext';
import { AUTHOR_EVENT, loadAuthor } from './useSavedAuthor';
import { loadView, saveView } from './useView';
import type { View } from './useView';

const has = (d: CommentPage, id: string) => d.items.some((r) => r.id === id || r.descendants.some((x) => x.id === id));

export default function App() {
  const [sortBy, setSortBy] = useState<SortBy>('CREATED_AT');
  const [desc, setDesc] = useState(true); // по умолчанию LIFO
  const [page, setPage] = useState(1);
  const [data, setData] = useState<CommentPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(0); // новые комментарии, которые не попали в текущий вид
  const [view, setView] = useState<View>(loadView);
  const [me, setMe] = useState<Me | null>(() => {
    const a = loadAuthor();
    return a ? { userName: a.userName, email: a.email } : null;
  });
  useEffect(() => {
    const sync = () => { const a = loadAuthor(); setMe(a ? { userName: a.userName, email: a.email } : null); };
    window.addEventListener(AUTHOR_EVENT, sync);
    return () => window.removeEventListener(AUTHOR_EVENT, sync);
  }, []);
  const [composeOpen, setComposeOpen] = useState(false);
  const [showFab, setShowFab] = useState(false);
  const [lightbox, setLightbox] = useState<LightboxItem | null>(null);
  const [fresh, setFresh] = useState<Set<string>>(new Set());
  const [searchInput, setSearchInput] = useState('');
  const [query, setQuery] = useState('');
  const seq = useRef(0);
  const dataRef = useRef<CommentPage | null>(null);
  const viewRef = useRef({ sortBy, desc, page });
  useEffect(() => { dataRef.current = data; }, [data]);
  useEffect(() => { viewRef.current = { sortBy, desc, page }; }, [sortBy, desc, page]);

  const load = useCallback(async () => {
    const id = ++seq.current; // защита от «обогнавших» друг друга ответов
    try {
      const d = await fetchCommentsPage(sortBy, desc, page);
      if (id !== seq.current) return;
      setData(d);
      setPending(0);
      setError(null);
    } catch (e) {
      if (id === seq.current) setError(e instanceof Error ? e.message : 'Failed to load comments');
    }
  }, [sortBy, desc, page]);
  useEffect(() => { void load(); }, [load]);

  const loadRef = useRef(load);
  useEffect(() => { loadRef.current = load; }, [load]);

  const markFresh = useCallback((c: ApiComment) => setFresh((prev) => new Set(prev).add(c.id)), []);

  /**
   * Live-событие применяется локально, БЕЗ запроса к серверу: при тысячах онлайн-клиентов
   * перечитывание списка на каждое событие создало бы лавину запросов.
   */
  const applyCreated = useCallback((c: ApiComment) => {
    const now = dataRef.current;
    if (!now || has(now, c.id)) return;
    const v = viewRef.current;

    if (c.parentId === null) {
      if (v.page === 1 && v.sortBy === 'CREATED_AT' && v.desc) {
        markFresh(c);
        setData((prev) => prev && !has(prev, c.id)
          ? { ...prev, totalCount: prev.totalCount + 1, items: [{ ...c, descendants: [] }, ...prev.items].slice(0, prev.pageSize) }
          : prev);
      } else {
        setPending((n) => n + 1); // другая сортировка/страница: предлагаем обновить вручную
      }
      return;
    }

    const rootId = now.items.find((r) => r.id === c.parentId || r.descendants.some((d) => d.id === c.parentId))?.id;
    if (!rootId) return; // ответ в ветке, которой нет на этой странице
    markFresh(c);
    setData((prev) => prev && !has(prev, c.id)
      ? { ...prev, items: prev.items.map((r) => (r.id === rootId ? { ...r, descendants: [...r.descendants, c] } : r)) }
      : prev);
  }, [markFresh]);

  useCommentsHub((c) => {
    if (c) applyCreated(c);
    else window.setTimeout(() => void loadRef.current(), Math.random() * 5000); // реконнект: перечитать со случайной задержкой
  });

  const onCreated = useCallback((c: ApiComment) => { markFresh(c); void loadRef.current(); }, [markFresh]);

  // Кнопка «New comment» появляется, когда форма вверху страницы уже не видна.
  useEffect(() => {
    const onScroll = () => setShowFab(window.scrollY > 400);
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  const closeCompose = useCallback(() => setComposeOpen(false), []);

  /** Новый корневой комментарий из окна: показываем начало списка (новые сверху), чтобы пользователь его увидел. */
  const onComposed = useCallback((c: ApiComment) => {
    setComposeOpen(false);
    setSortBy('CREATED_AT'); setDesc(true); setPage(1);
    markFresh(c);
    void loadRef.current();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, [markFresh]);

  useEffect(() => {
    const t = window.setTimeout(() => setQuery(searchInput.trim()), 350);
    return () => window.clearTimeout(t);
  }, [searchInput]);

  function onSort(col: SortBy) {
    if (col === sortBy) setDesc((d) => !d);
    else { setSortBy(col); setDesc(col === 'CREATED_AT'); }
    setPage(1);
  }

  const changeView = (v: View) => { setView(v); saveView(v); };

  const nodes = useMemo(() => (data ? data.items.map(buildNode) : []), [data]);
  const totalPages = data ? Math.ceil(data.totalCount / data.pageSize) : 0;

  return (
    <MeContext.Provider value={me}>
    <div className="container">
      <header className="top">
        <h1>Comments</h1>
        <input type="search" className="search" placeholder="Search comments…" value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)} aria-label="Search comments" />
      </header>

      {query && <SearchPanel key={query} query={query} />}

      <section className="card">
        <h2>Leave a comment</h2>
        <CommentForm parentId={null} onCreated={onCreated} />
      </section>

      <section>
        <h2>Discussion {data && <span className="count">{data.totalCount}</span>}</h2>
        <SortBar sortBy={sortBy} desc={desc} onSort={onSort} view={view} onView={changeView} />
        {pending > 0 && (
          <button className="banner" onClick={() => void load()}>
            {pending} new comment{pending > 1 ? 's' : ''} — click to refresh
          </button>
        )}
        {error && <p className="err" role="alert">{error}</p>}
        {data && nodes.length === 0 && <p className="muted">No comments yet. Be the first!</p>}
        {nodes.length > 0 && (
          view === 'cards'
            ? <CommentCards items={nodes} fresh={fresh} onOpen={setLightbox} onCreated={onCreated} />
            : <CommentTable items={nodes} fresh={fresh} onOpen={setLightbox} onCreated={onCreated} />
        )}
        <Pagination page={page} totalPages={totalPages} onChange={(p) => { setPage(p); window.scrollTo({ top: 0, behavior: 'smooth' }); }} />
      </section>

      {showFab && !composeOpen && (
        <button type="button" className="fab primary" onClick={() => setComposeOpen(true)}>✎ New comment</button>
      )}
      <ComposeDialog open={composeOpen} onClose={closeCompose} onCreated={onComposed} />

      {lightbox && <Lightbox item={lightbox} onClose={() => setLightbox(null)} />}
    </div>
    </MeContext.Provider>
  );
}
