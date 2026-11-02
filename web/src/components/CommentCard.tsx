import { useState } from 'react';
import type { ApiComment, CommentNode } from '../types';
import { CommentForm } from './CommentForm';
import { CommentThread } from './CommentThread';
import { Attachments, Author, formatDate } from './CommentParts';
import type { LightboxItem } from './Lightbox';
import { SafeHtml } from './SafeHtml';

export interface CardProps {
  node: CommentNode;
  side: 'left' | 'right';
  fresh: Set<string>;
  onOpen: (i: LightboxItem) => void;
  onCreated: (c: ApiComment) => void;
}

/** Номер цвета аватара (0..7) по имени: один и тот же автор всегда одного цвета. */
function colorIndex(name: string): number {
  let h = 0;
  for (const ch of name) h = (h * 31 + ch.charCodeAt(0)) >>> 0;
  return h % 8;
}

const countReplies = (n: CommentNode): number => n.replies.reduce((sum, r) => sum + 1 + countReplies(r), 0);

export function CommentCard({ node, side, fresh, onOpen, onCreated }: CardProps) {
  const [replying, setReplying] = useState(false);
  const replies = countReplies(node);

  return (
    <div className={`card-row ${side}`}>
      <article className={'bubble' + (fresh.has(node.id) ? ' fresh' : '')}>
        <header className="bubble-head">
          <span className={`avatar c${colorIndex(node.userName)}`} aria-hidden="true">
            {node.userName.charAt(0).toUpperCase()}
          </span>
          <div className="who">
            <strong><Author c={node} />{side === 'right' && <span className="badge">you</span>}</strong>
            <span className="muted">{node.email}</span>
          </div>
          <time className="muted">{formatDate(node.createdAt)}</time>
        </header>

        <SafeHtml html={node.textHtml} className="text" />
        <Attachments items={node.attachments} onOpen={onOpen} />

        <div className="bubble-actions">
          <button className="link" onClick={() => setReplying((v) => !v)}>{replying ? 'Cancel' : 'Reply'}</button>
          {replies > 0 && <span className="muted">{replies} {replies === 1 ? 'reply' : 'replies'}</span>}
        </div>

        {replying && (
          <CommentForm parentId={node.id} onCancel={() => setReplying(false)}
            onCreated={(c) => { setReplying(false); onCreated(c); }} />
        )}

        {node.replies.length > 0 && (
          <div className="replies">
            {node.replies.map((r) => (
              <CommentThread key={r.id} node={r} depth={1} fresh={fresh} onOpen={onOpen} onCreated={onCreated} />
            ))}
          </div>
        )}
      </article>
    </div>
  );
}
