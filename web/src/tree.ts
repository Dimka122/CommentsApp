import type { ApiComment, CommentNode, RootWithDescendants } from './types';

/** Собирает дерево из корня и плоского списка descendants (порядок уже «от старых к новым»). */
export function buildNode(root: RootWithDescendants): CommentNode {
  const { descendants, ...base } = root;
  const rootNode: CommentNode = { ...base, replies: [] };
  const nodes = new Map<string, CommentNode>([[rootNode.id, rootNode]]);
  const flat: ApiComment[] = descendants;
  for (const d of flat) nodes.set(d.id, { ...d, replies: [] });
  for (const d of flat) {
    const parent = d.parentId ? nodes.get(d.parentId) : undefined;
    const node = nodes.get(d.id);
    if (parent && node) parent.replies.push(node);
  }
  return rootNode;
}
