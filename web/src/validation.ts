import type { FieldErrors } from './types';

export interface FormValues {
  userName: string; email: string; homepage: string; text: string; captchaAnswer: string; file: File | null;
}

const USER_NAME = /^[A-Za-z0-9]{1,64}$/;
const EMAIL = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const CAPTCHA = /^[A-Za-z0-9]{1,16}$/;
const ALLOWED_TAG = /^<(\/)?(a|code|i|strong)(\s[^<>]*)?>$/i;

/** Проверка закрытия и вложенности разрешённых тегов (как на сервере). null = всё хорошо. */
export function checkTags(text: string): string | null {
  const stack: string[] = [];
  for (const tag of text.match(/<[^<>]*>/g) ?? []) {
    const m = ALLOWED_TAG.exec(tag);
    if (!m) continue;
    const name = m[2].toLowerCase();
    if (m[1]) {
      if (stack.pop() !== name) return `Unexpected closing tag </${name}>.`;
    } else {
      stack.push(name);
    }
  }
  return stack.length ? `Tag <${stack[stack.length - 1]}> is not closed.` : null;
}

export function validateForm(v: FormValues): FieldErrors {
  const e: FieldErrors = {};
  const add = (key: string, msg: string) => { (e[key] ??= []).push(msg); };

  if (!v.userName.trim()) add('userName', 'User name is required.');
  else if (!USER_NAME.test(v.userName.trim())) add('userName', 'Use only Latin letters and digits.');

  if (!v.email.trim()) add('email', 'E-mail is required.');
  else if (!EMAIL.test(v.email.trim())) add('email', 'E-mail format is invalid.');

  if (v.homepage.trim()) {
    try {
      const u = new URL(v.homepage.trim());
      if (u.protocol !== 'http:' && u.protocol !== 'https:') throw new Error('scheme');
    } catch { add('homepage', 'Homepage must be a valid http(s) URL.'); }
  }

  if (!v.text.trim()) add('text', 'Text is required.');
  else if (v.text.length > 5000) add('text', 'Text is too long (max 5000 characters).');
  else { const t = checkTags(v.text); if (t) add('text', t); }

  if (!v.captchaAnswer.trim()) add('captchaAnswer', 'CAPTCHA is required.');
  else if (!CAPTCHA.test(v.captchaAnswer.trim())) add('captchaAnswer', 'CAPTCHA contains only Latin letters and digits.');

  if (v.file) {
    const name = v.file.name.toLowerCase();
    const dot = name.lastIndexOf('.');
    const ext = dot >= 0 ? name.slice(dot) : '';
    if (ext === '.txt') {
      if (v.file.size > 100 * 1024) add('file', 'Text file must not exceed 100 KB.');
    } else if (['.jpg', '.jpeg', '.png', '.gif'].includes(ext)) {
      if (v.file.size > 5 * 1024 * 1024) add('file', 'Image must not exceed 5 MB.');
    } else {
      add('file', 'Allowed files: JPG, GIF, PNG images or TXT up to 100 KB.');
    }
  }
  return e;
}
