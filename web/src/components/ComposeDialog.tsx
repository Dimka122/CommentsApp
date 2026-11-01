import { useEffect, useState } from 'react';
import type { ApiComment } from '../types';
import { CommentForm } from './CommentForm';

interface Props { open: boolean; onClose: () => void; onCreated: (c: ApiComment) => void }

/**
 * Окно «Новый комментарий» поверх страницы: открывается кнопкой, которая всегда под рукой,
 * поэтому не нужно возвращаться наверх. После первого открытия остаётся в DOM (скрытым),
 * чтобы набранный черновик не пропадал при закрытии.
 */
export function ComposeDialog({ open, onClose, onCreated }: Props) {
  const [everOpened, setEverOpened] = useState(open);
  useEffect(() => { if (open) setEverOpened(true); }, [open]);

  useEffect(() => {
    if (!open) return;
    document.body.classList.add('no-scroll');
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => {
      document.body.classList.remove('no-scroll');
      window.removeEventListener('keydown', onKey);
    };
  }, [open, onClose]);

  if (!everOpened) return null;
  return (
    <div className="compose-overlay" hidden={!open} role="dialog" aria-modal="true" aria-label="New comment">
      <div className="compose-panel">
        <header className="compose-head">
          <h2>New comment</h2>
          <button type="button" className="icon" onClick={onClose} aria-label="Close">×</button>
        </header>
        <CommentForm parentId={null} autoFocus={open} onCreated={onCreated} onCancel={onClose} />
      </div>
    </div>
  );
}
