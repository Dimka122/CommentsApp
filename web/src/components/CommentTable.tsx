import { useState } from 'react';
import type { ApiComment, CommentNode } from '../types';
import { CommentForm } from './CommentForm';
import { CommentThread } from './CommentThread';
import { Attachments, Author, formatDate } from './CommentParts';
import type { LightboxItem } from './Lightbox';
import { SafeHtml } from './SafeHtml';

interface Props {
  items: CommentNode[];
  fresh: Set<string>;
  onOpen: (i: LightboxItem) => void;
  onCreated: (c: ApiComment) => void;
}

function RootRow({ node, fresh, onOpen, onCreated }: Omit<Props, 'items'> & { node: CommentNode }) {
  const [replying, setReplying] = useState(false);
  const hasThread = replying || node.replies.length > 0;
  return (
    <tbody className={'root' + (fresh.has(node.id) ? ' fresh' : '')}>
      <tr>
        <td className="author-cell">
          <strong><Author c={node} /></strong>
          <div className="muted">{node.email}</div>
          <time className="muted">{formatDate(node.createdAt)}</time>
        </td>
        <td>
          <SafeHtml html={node.textHtml} className="text" />
          <Attachments items={node.attachments} onOpen={onOpen} />
          <button className="link" onClick={() => setReplying((v) => !v)}>{replying ? 'Cancel' : 'Reply'}</button>
        </td>
      </tr>
      {hasThread && (
        <tr className="thread">
          <td colSpan={2}>
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
          </td>
        </tr>
      )}
    </tbody>
  );
}

/** Таблица корневых комментариев. Шапка скрыта визуально (остаётся для скринридеров), сортировка — в SortBar. */
export function CommentTable({ items, fresh, onOpen, onCreated }: Props) {
  return (
    <div className="table-wrap">
      <table className="comments">
        <thead className="sr-only">
          <tr><th>Author</th><th>Comment</th></tr>
        </thead>
        {items.map((n) => (
          <RootRow key={n.id} node={n} fresh={fresh} onOpen={onOpen} onCreated={onCreated} />
        ))}
      </table>
    </div>
  );
}
