// Запоминание автора в localStorage этого браузера, чтобы не вводить имя/e-mail при каждом комментарии.
const KEY = 'comments.author';

export interface SavedAuthor { userName: string; email: string; homepage: string }

export function loadAuthor(): SavedAuthor | null {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    const v = JSON.parse(raw);
    if (typeof v?.userName === 'string' && typeof v?.email === 'string' && v.userName && v.email) {
      return { userName: v.userName, email: v.email, homepage: typeof v.homepage === 'string' ? v.homepage : '' };
    }
  } catch { /* хранилище недоступно или повреждено */ }
  return null;
}

export function saveAuthor(a: SavedAuthor) {
  try { localStorage.setItem(KEY, JSON.stringify(a)); } catch { /* ignore */ }
}

export function forgetAuthor() {
  try { localStorage.removeItem(KEY); } catch { /* ignore */ }
}
