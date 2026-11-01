import { describe, expect, it } from 'vitest';
import { checkTags, validateForm } from './validation';

const valid = { userName: 'John123', email: 'john@example.com', homepage: '', text: 'hello', captchaAnswer: 'A1B2C', file: null };

describe('checkTags', () => {
  it('accepts balanced tags', () => expect(checkTags('<i>a <strong>b</strong></i>')).toBeNull());
  it('rejects unclosed tag', () => expect(checkTags('<i>a')).not.toBeNull());
  it('rejects wrong nesting', () => expect(checkTags('<i><strong>a</i></strong>')).not.toBeNull());
  it('ignores unknown tags (server escapes them)', () => expect(checkTags('<b>a</b>')).toBeNull());
});

describe('validateForm', () => {
  it('accepts a valid form', () => expect(validateForm(valid)).toEqual({}));
  it('rejects non-latin user name', () => expect(validateForm({ ...valid, userName: 'Иван' }).userName).toBeDefined());
  it('rejects bad e-mail', () => expect(validateForm({ ...valid, email: 'nope' }).email).toBeDefined());
  it('homepage is optional but must be http(s)', () => {
    expect(validateForm({ ...valid, homepage: '' })).toEqual({});
    expect(validateForm({ ...valid, homepage: 'javascript:alert(1)' }).homepage).toBeDefined();
  });
  it('requires captcha', () => expect(validateForm({ ...valid, captchaAnswer: '' }).captchaAnswer).toBeDefined());
  it('limits text file to 100 KB', () => {
    const big = new File([new Uint8Array(101 * 1024)], 'a.txt');
    expect(validateForm({ ...valid, file: big }).file).toBeDefined();
  });
  it('rejects other extensions', () => {
    expect(validateForm({ ...valid, file: new File(['x'], 'a.exe') }).file).toBeDefined();
  });
});
