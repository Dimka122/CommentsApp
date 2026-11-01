import { useEffect, useState } from 'react';

export interface LightboxItem { kind: 'image' | 'text'; url: string; title: string }

export function Lightbox({ item, onClose }: { item: LightboxItem; onClose: () => void }) {
  const [text, setText] = useState<string | null>(null);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  useEffect(() => {
    if (item.kind !== 'text') return;
    let cancelled = false;
    setText(null);
    fetch(item.url)
      .then((r) => r.text())
      .then((t) => { if (!cancelled) setText(t); })
      .catch(() => { if (!cancelled) setText('Failed to load file.'); });
    return () => { cancelled = true; };
  }, [item]);

  return (
    <div className="lb-overlay" onClick={onClose} role="dialog" aria-modal="true" aria-label={item.title}>
      <figure className="lb-content" onClick={(e) => e.stopPropagation()}>
        {item.kind === 'image'
          ? <img className="lb-image" src={item.url} alt={item.title} />
          : <pre className="lb-text">{text ?? 'Loading…'}</pre>}
        <figcaption>{item.title}</figcaption>
        <button className="lb-close" onClick={onClose} aria-label="Close">×</button>
      </figure>
    </div>
  );
}
