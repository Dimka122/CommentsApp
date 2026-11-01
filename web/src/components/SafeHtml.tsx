import { useMemo } from 'react';
import DOMPurify from 'dompurify';

const CONFIG = {
  ALLOWED_TAGS: ['a', 'code', 'i', 'strong', 'br', 'mark'],
  ALLOWED_ATTR: ['href', 'title', 'rel', 'target'],
};

/** Сервер уже санитизирует HTML; здесь вторая линия защиты (defense in depth) перед вставкой в DOM. */
export function SafeHtml({ html, className }: { html: string; className?: string }) {
  const clean = useMemo(() => DOMPurify.sanitize(html, CONFIG), [html]);
  return <div className={className} dangerouslySetInnerHTML={{ __html: clean }} />;
}
