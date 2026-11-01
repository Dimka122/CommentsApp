import type { ApiComment, Attachment } from '../types';
import type { LightboxItem } from './Lightbox';

const formatSize = (b: number) => (b < 1024 ? `${b} B` : `${(b / 1024).toFixed(1)} KB`);
export const formatDate = (iso: string) => new Date(iso).toLocaleString();

export function Author({ c }: { c: ApiComment }) {
  const safeLink = c.homepage && /^https?:\/\//i.test(c.homepage) ? c.homepage : null;
  return safeLink
    ? <a href={safeLink} rel="nofollow noopener noreferrer" target="_blank">{c.userName}</a>
    : <span>{c.userName}</span>;
}

export function Attachments({ items, onOpen }: { items: Attachment[]; onOpen: (i: LightboxItem) => void }) {
  if (items.length === 0) return null;
  return (
    <div className="attachments">
      {items.map((a) => {
        const url = `/api/attachments/${a.id}`;
        return a.kind.toLowerCase() === 'image' ? (
          <button key={a.id} className="thumb" onClick={() => onOpen({ kind: 'image', url, title: a.originalName })}>
            <img src={url} alt={a.originalName} loading="lazy" />
          </button>
        ) : (
          <button key={a.id} className="file" onClick={() => onOpen({ kind: 'text', url, title: a.originalName })}>
            📄 {a.originalName} <small>({formatSize(a.sizeBytes)})</small>
          </button>
        );
      })}
    </div>
  );
}
