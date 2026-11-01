import type { ApiComment, Captcha, CommentPage, FieldErrors, SearchPage, SortBy } from './types';

export class ApiError extends Error {
  constructor(public status: number, public fieldErrors: FieldErrors, message: string) {
    super(message);
  }
}

async function toError(res: Response): Promise<ApiError> {
  let fieldErrors: FieldErrors = {};
  let message = `Request failed (${res.status})`;
  try {
    const body = await res.json();
    if (body?.errors) fieldErrors = body.errors;
    if (body?.title) message = body.title;
  } catch { /* тело не JSON */ }
  if (res.status === 429) message = 'Too many requests. Please wait a minute and try again.';
  return new ApiError(res.status, fieldErrors, message);
}

export async function fetchCaptcha(): Promise<Captcha> {
  const res = await fetch('/api/captcha');
  if (!res.ok) throw await toError(res);
  return res.json();
}

export async function previewText(text: string): Promise<string> {
  const res = await fetch('/api/comments/preview', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ text }),
  });
  if (!res.ok) throw await toError(res);
  return (await res.json()).html as string;
}

export async function createComment(form: FormData): Promise<ApiComment> {
  const res = await fetch('/api/comments', { method: 'POST', body: form });
  if (!res.ok) throw await toError(res);
  return res.json();
}

const COMMENTS_QUERY = `
query Comments($sortBy: CommentSortBy!, $desc: Boolean!, $page: Int!) {
  comments(sortBy: $sortBy, desc: $desc, page: $page) {
    totalCount page pageSize
    items { ...F descendants { ...F } }
  }
}
fragment F on Comment {
  id parentId userName email homepage textHtml createdAt
  attachments { id kind originalName sizeBytes }
}`;

export async function fetchCommentsPage(sortBy: SortBy, desc: boolean, page: number): Promise<CommentPage> {
  const res = await fetch('/graphql', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ query: COMMENTS_QUERY, variables: { sortBy, desc, page } }),
  });
  if (!res.ok) throw await toError(res);
  const body = (await res.json()) as { data?: { comments: CommentPage }; errors?: { message: string }[] };
  if (body.errors?.length || !body.data) throw new Error(body.errors?.[0]?.message ?? 'GraphQL error');
  return body.data.comments;
}

export async function searchComments(q: string, page: number): Promise<SearchPage> {
  const res = await fetch(`/api/search?q=${encodeURIComponent(q)}&page=${page}`);
  if (!res.ok) throw await toError(res);
  return res.json();
}
