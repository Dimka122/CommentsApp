export type View = 'cards' | 'table';
const KEY = 'comments.view';

export function loadView(): View {
  try { return localStorage.getItem(KEY) === 'table' ? 'table' : 'cards'; } catch { return 'cards'; }
}
export function saveView(v: View) {
  try { localStorage.setItem(KEY, v); } catch { /* ignore */ }
}
