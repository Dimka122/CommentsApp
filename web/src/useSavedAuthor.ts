// Автор запоминается по умолчанию только для текущей вкладки (sessionStorage): в новой вкладке — чистая форма,
// можно писать от другого имени. Если включить «Remember me», данные дополнительно сохраняются на устройстве
// (localStorage) и подставляются во всех новых вкладках.
const KEY = 'comments.author';

/** Событие window: автор изменился (отправили комментарий или нажали «Forget me»). */
export const AUTHOR_EVENT = 'comments-author-changed';

export interface SavedAuthor { userName: string; email: string; homepage: string }

function read(store: () => Storage): SavedAuthor | null {
  try {
    const raw = store().getItem(KEY);
    if (!raw) return null;
    const v = JSON.parse(raw);
    if (typeof v?.userName === 'string' && typeof v?.email === 'string' && v.userName && v.email) {
      return { userName: v.userName, email: v.email, homepage: typeof v.homepage === 'string' ? v.homepage : '' };
    }
  } catch { /* хранилище недоступно или повреждено */ }
  return null;
}

export function loadAuthor(): SavedAuthor | null {
  return read(() => sessionStorage) ?? read(() => localStorage);
}

/** persistent=true — запомнить ещё и на устройстве (для всех вкладок). */
export function saveAuthor(a: SavedAuthor, persistent: boolean) {
  const json = JSON.stringify(a);
  try { sessionStorage.setItem(KEY, json); } catch { /* ignore */ }
  if (persistent) { try { localStorage.setItem(KEY, json); } catch { /* ignore */ } }
  window.dispatchEvent(new Event(AUTHOR_EVENT));
}

export function forgetAuthor() {
  try { sessionStorage.removeItem(KEY); } catch { /* ignore */ }
  try { localStorage.removeItem(KEY); } catch { /* ignore */ }
  window.dispatchEvent(new Event(AUTHOR_EVENT));
}
