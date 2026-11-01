import { useState } from 'react';
import type { ApiComment, CommentNode, SortBy } from '../types';
import { CommentForm } from './CommentForm';
import { CommentThread } from './CommentThread';
import { Attachments, Author, formatDate } from './CommentParts';
import type { LightboxItem } from './Lightbox';
import { SafeHtml } from './SafeHtml';

interface Props {
  items: CommentNode[];
  sortBy: SortBy;
  desc: boolean;
  onSort: (col: SortBy) => void;
  fresh: Set<string>;
  onOpen: (i: LightboxItem) => void;
  onCreated: (c: ApiComment) => void;
}

const arrow = (active: boolean, desc: boolean) => (active ? (desc ? ' ▼' : ' ▲') : '');

function RootRow({ node, fresh, onOpen, onCreated }: Omit<Props, 'items' | 'sortBy' | 'desc' | 'onSort'> & { node: CommentNode }) {
  const [replying, setReplying] = useState(false);
  const hasThread = replying || node.replies.length > 0;
  return (
    <tbody className={'root' + (fresh.has(node.id) ? ' fresh' : '')}>
      <tr>
        <td className="nowrap"><strong><Author c={node} /></strong></td>
        <td className="nowrap">{node.email}</td>
        <td className="nowrap muted">{formatDate(node.createdAt)}</td>
        <td>
          <SafeHtml html={node.textHtml} className="text" />
          <Attachments items={node.attachments} onOpen={onOpen} />
          <button className="link" onClick={() => setReplying((v) => !v)}>{replying ? 'Cancel' : 'Reply'}</button>
        </td>
      </tr>
      {hasThread && (
        <tr className="thread">
          <td colSpan={4}>
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

export function CommentTable(p: Props) {
  const th = (col: SortBy, label: string) => (
    <th aria-sort={p.sortBy === col ? (p.desc ? 'descending' : 'ascending') : 'none'}>
      <button className="sort" onClick={() => p.onSort(col)}>{label}{arrow(p.sortBy === col, p.desc)}</button>
    </th>
  );
  return (
    <div className="table-wrap">
      <table className="comments">
        <thead>
          <tr>{th('USER_NAME', 'User Name')}{th('EMAIL', 'E-mail')}{th('CREATED_AT', 'Date')}<th>Comment</th></tr>
        </thead>
        {p.items.map((n) => (
          <RootRow key={n.id} node={n} fresh={p.fresh} onOpen={p.onOpen} onCreated={p.onCreated} />
        ))}
      </table>
    </div>
  );
}
