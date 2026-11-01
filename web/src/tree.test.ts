import { describe, expect, it } from 'vitest';
import { buildNode } from './tree';
import type { ApiComment } from './types';

const c = (id: string, parentId: string | null): ApiComment => ({
  id, parentId, userName: id, email: `${id}@e.com`, homepage: null, textHtml: id, createdAt: '2026-01-01T00:00:00Z', attachments: [],
});

describe('buildNode', () => {
  it('rebuilds the tree from a flat DFS list', () => {
    const root = { ...c('root', null), descendants: [c('a', 'root'), c('a1', 'a'), c('b', 'root')] };
    const node = buildNode(root);
    expect(node.replies.map((r) => r.id)).toEqual(['a', 'b']);
    expect(node.replies[0].replies.map((r) => r.id)).toEqual(['a1']);
  });
  it('handles a root without replies', () => expect(buildNode({ ...c('x', null), descendants: [] }).replies).toEqual([]));
});
