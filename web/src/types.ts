export interface Attachment { id: string; kind: string; originalName: string; sizeBytes: number }

export interface ApiComment {
  id: string;
  parentId: string | null;
  userName: string;
  email: string;
  homepage: string | null;
  textHtml: string;
  createdAt: string;
  attachments: Attachment[];
}

export type RootWithDescendants = ApiComment & { descendants: ApiComment[] };

export interface CommentNode extends ApiComment { replies: CommentNode[] }

export interface CommentPage { totalCount: number; page: number; pageSize: number; items: RootWithDescendants[] }

export type SortBy = 'CREATED_AT' | 'USER_NAME' | 'EMAIL';

export interface SearchHit { id: string; parentId: string | null; userName: string; snippet: string; createdAt: string }
export interface SearchPage { items: SearchHit[]; page: number; pageSize: number; totalCount: number }

export interface Captcha { id: string; imageDataUri: string }

export type FieldErrors = Record<string, string[]>;
