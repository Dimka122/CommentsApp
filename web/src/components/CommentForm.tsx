import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { ApiError, createComment, fetchCaptcha, previewText } from '../api';
import type { ApiComment, Captcha, FieldErrors } from '../types';
import { forgetAuthor, loadAuthor, saveAuthor } from '../useSavedAuthor';
import { checkTags, validateForm } from '../validation';
import { SafeHtml } from './SafeHtml';

interface Props {
  parentId: string | null;
  onCreated: (c: ApiComment) => void;
  onCancel?: () => void;
  autoFocus?: boolean;
}

function Field({ label, error, required, children }: { label: string; error?: string; required?: boolean; children: ReactNode }) {
  return (
    <label className={'field' + (error ? ' invalid' : '')}>
      <span className="label">{label}{required && <b> *</b>}</span>
      {children}
      {error && <small className="err">{error}</small>}
    </label>
  );
}

export function CommentForm({ parentId, onCreated, onCancel, autoFocus }: Props) {
  const saved = useMemo(loadAuthor, []);
  const [userName, setUserName] = useState(saved?.userName ?? '');
  const [email, setEmail] = useState(saved?.email ?? '');
  const [homepage, setHomepage] = useState(saved?.homepage ?? '');
  const [editingAuthor, setEditingAuthor] = useState(saved === null);
  const [remember, setRemember] = useState(false);
  const [text, setText] = useState('');
  const [captchaAnswer, setCaptchaAnswer] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [captcha, setCaptcha] = useState<Captcha | null>(null);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [previewHtml, setPreviewHtml] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const textRef = useRef<HTMLTextAreaElement>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  const loadCaptcha = useCallback(async () => {
    try { setCaptcha(await fetchCaptcha()); }
    catch { setFormError('Could not load CAPTCHA. Please try again.'); }
  }, []);
  useEffect(() => { void loadCaptcha(); }, [loadCaptcha]);
  useEffect(() => { if (autoFocus) textRef.current?.focus(); }, [autoFocus]);

  const err = (k: string) => errors[k]?.[0];

  /** Ошибки в полях автора возможны, только когда они видны — раскрываем их. */
  function showErrors(e: FieldErrors) {
    setErrors(e);
    if (e.userName || e.email || e.homepage) setEditingAuthor(true);
  }

  function handleError(e: unknown) {
    if (e instanceof ApiError) {
      showErrors(e.fieldErrors);
      setFormError(Object.keys(e.fieldErrors).length ? null : e.message);
    } else {
      setFormError('Network error. Please try again.');
    }
  }

  function forget() {
    forgetAuthor();
    setUserName(''); setEmail(''); setHomepage('');
    setEditingAuthor(true);
  }

  /** Кнопки панели: оборачивают выделенный текст в тег. */
  function wrap(open: string, close: string, placeholder: string, select?: [number, number]) {
    const el = textRef.current;
    if (!el) return;
    const s = el.selectionStart, e = el.selectionEnd;
    const selected = text.slice(s, e) || placeholder;
    setText(text.slice(0, s) + open + selected + close + text.slice(e));
    const [from, to] = select ?? [open.length, open.length + selected.length];
    requestAnimationFrame(() => { el.focus(); el.setSelectionRange(s + from, s + to); });
  }

  async function preview() {
    setFormError(null);
    if (!text.trim()) { setErrors({ text: ['Text is required.'] }); return; }
    const tagError = checkTags(text);
    if (tagError) { setErrors({ text: [tagError] }); return; }
    try {
      setPreviewHtml(await previewText(text));
      setErrors((prev) => ({ ...prev, text: [] }));
    } catch (e) { handleError(e); }
  }

  async function submit(ev: FormEvent) {
    ev.preventDefault();
    setFormError(null);
    const found = validateForm({ userName, email, homepage, text, captchaAnswer, file });
    if (Object.keys(found).length > 0 || !captcha) {
      showErrors(found);
      if (!captcha) setFormError('CAPTCHA is not loaded yet.');
      return;
    }
    setErrors({});
    setBusy(true);
    try {
      const fd = new FormData();
      fd.append('userName', userName.trim());
      fd.append('email', email.trim());
      if (homepage.trim()) fd.append('homepage', homepage.trim());
      fd.append('text', text);
      if (parentId) fd.append('parentId', parentId);
      fd.append('captchaId', captcha.id);
      fd.append('captchaAnswer', captchaAnswer.trim());
      if (file) fd.append('file', file);

      const created = await createComment(fd);

      saveAuthor({ userName: userName.trim(), email: email.trim(), homepage: homepage.trim() }, remember);
      setEditingAuthor(false);
      setText(''); setFile(null); setPreviewHtml(null);
      if (fileRef.current) fileRef.current.value = '';
      onCreated(created);
    } catch (e) {
      handleError(e);
    } finally {
      setBusy(false);
      setCaptchaAnswer('');
      void loadCaptcha(); // CAPTCHA одноразовая: после любой попытки нужна новая
    }
  }

  return (
    <form className={'comment-form' + (parentId ? ' reply-form' : '')} onSubmit={submit} noValidate>
      {editingAuthor ? (
        <>
          <div className="grid">
            <Field label="User Name" required error={err('userName')}>
              <input value={userName} maxLength={64} autoComplete="nickname" onChange={(e) => setUserName(e.target.value)} />
            </Field>
            <Field label="E-mail" required error={err('email')}>
              <input type="email" value={email} maxLength={254} autoComplete="email" onChange={(e) => setEmail(e.target.value)} />
            </Field>
            <Field label="Homepage" error={err('homepage')}>
              <input type="url" value={homepage} maxLength={2048} placeholder="https://" onChange={(e) => setHomepage(e.target.value)} />
            </Field>
          </div>
          <label className="remember">
            <input type="checkbox" checked={remember} onChange={(e) => setRemember(e.target.checked)} />
            Remember me on this device (all tabs)
          </label>
        </>
      ) : (
        <div className="author-summary">
          Posting as <strong>{userName}</strong> <span className="muted">({email})</span>
          <button type="button" className="link" onClick={() => setEditingAuthor(true)}>Change</button>
          <button type="button" className="link" onClick={forget}>Forget me</button>
        </div>
      )}

      <Field label="Text" required error={err('text')}>
        <div className="toolbar" role="toolbar" aria-label="Formatting">
          <button type="button" onClick={() => wrap('<i>', '</i>', 'text')}>[i]</button>
          <button type="button" onClick={() => wrap('<strong>', '</strong>', 'text')}>[strong]</button>
          <button type="button" onClick={() => wrap('<code>', '</code>', 'code')}>[code]</button>
          <button type="button" onClick={() => wrap('<a href="https://" title="">', '</a>', 'link', [9, 17])}>[a]</button>
        </div>
        <textarea ref={textRef} rows={parentId ? 3 : 5} maxLength={5000} value={text} onChange={(e) => setText(e.target.value)} />
      </Field>

      {previewHtml !== null && (
        <div className="preview">
          <div className="label">Preview</div>
          <SafeHtml html={previewHtml} className="text" />
        </div>
      )}

      <div className="grid">
        <Field label="File (JPG, GIF, PNG ≤ 320×240 or TXT ≤ 100 KB)" error={err('file')}>
          <input ref={fileRef} type="file" accept=".jpg,.jpeg,.png,.gif,.txt" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
        </Field>
        <Field label="CAPTCHA" required error={err('captchaAnswer')}>
          <div className="captcha">
            {captcha ? <img src={captcha.imageDataUri} alt="CAPTCHA" width={170} height={60} /> : <div className="captcha-ph" />}
            <button type="button" className="icon" title="New CAPTCHA" onClick={() => void loadCaptcha()}>↻</button>
            <input value={captchaAnswer} maxLength={16} autoComplete="off" onChange={(e) => setCaptchaAnswer(e.target.value)} />
          </div>
        </Field>
      </div>

      {err('parentId') && <p className="err">{err('parentId')}</p>}
      {formError && <p className="err" role="alert">{formError}</p>}

      <div className="actions">
        <button type="button" onClick={() => void preview()}>Preview</button>
        <button type="submit" className="primary" disabled={busy}>{busy ? 'Sending…' : parentId ? 'Reply' : 'Send'}</button>
        {onCancel && <button type="button" onClick={onCancel}>Cancel</button>}
      </div>
    </form>
  );
}
