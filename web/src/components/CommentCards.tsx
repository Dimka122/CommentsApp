import { useIsMine } from '../AuthorContext';
import type { ApiComment, CommentNode } from '../types';
import { CommentCard } from './CommentCard';
import type { LightboxItem } from './Lightbox';

interface Props {
  items: CommentNode[];
  fresh: Set<string>;
  onOpen: (i: LightboxItem) => void;
  onCreated: (c: ApiComment) => void;
}

/** Как в мессенджере: ваши сообщения справа, чужие слева. */
export function CommentCards({ items, fresh, onOpen, onCreated }: Props) {
  const isMine = useIsMine();
  return (
    <div className="cards">
      {items.map((n) => (
        <CommentCard key={n.id} node={n} side={isMine(n) ? 'right' : 'left'}
          fresh={fresh} onOpen={onOpen} onCreated={onCreated} />
      ))}
    </div>
  );
}
