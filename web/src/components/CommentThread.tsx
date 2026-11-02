import { useState } from 'react';
import { useIsMine } from '../AuthorContext';
import type { ApiComment, CommentNode } from '../types';
import { CommentForm } from './CommentForm';
import { Attachments, Author, formatDate } from './CommentParts';
import type { LightboxItem } from './Lightbox';
import { SafeHtml } from './SafeHtml';

interface Props {
  node: CommentNode;
  depth: number;
  fresh: Set<string>;
  onOpen: (i: LightboxItem) => void;
  onCreated: (c: ApiComment) => void;
}

/** Ответ на комментарий (каскадное отображение, вложенность не ограничена). */
export function CommentThread({ node, depth, fresh, onOpen, onCreated }: Props) {
  const [replying, setReplying] = useState(false);
  const mine = useIsMine()(node);
  return (
    <article className={'comment' + (fresh.has(node.id) ? ' fresh' : '') + (mine ? ' mine' : '')}>
      <header className="meta">
        <strong><Author c={node} />{mine && <span className="badge">you</span>}</strong>
        <span className="muted">{node.email}</span>
        <time className="muted">{formatDate(node.createdAt)}</time>
        <button className="link" onClick={() => setReplying((v) => !v)}>{replying ? 'Cancel' : 'Reply'}</button>
      </header>
      <SafeHtml html={node.textHtml} className="text" />
      <Attachments items={node.attachments} onOpen={onOpen} />
      {replying && (
        <CommentForm parentId={node.id} onCancel={() => setReplying(false)}
          onCreated={(c) => { setReplying(false); onCreated(c); }} />
      )}
      {node.replies.length > 0 && (
        <div className={'replies' + (depth >= 5 ? ' flat' : '')}>
          {node.replies.map((r) => (
            <CommentThread key={r.id} node={r} depth={depth + 1} fresh={fresh} onOpen={onOpen} onCreated={onCreated} />
          ))}
        </div>
      )}
    </article>
  );
}
