import { createContext, useContext } from 'react';

export interface Me { userName: string; email: string }

/** Текущий автор этой вкладки (тот, под кем вы пишете). null — ещё ничего не отправляли. */
export const MeContext = createContext<Me | null>(null);

/** «Моё» сообщение = совпали имя и e-mail автора. Это не авторизация, а только признак для оформления. */
export function useIsMine() {
  const me = useContext(MeContext);
  return (c: { userName: string; email: string }) =>
    me !== null && c.userName === me.userName && c.email.toLowerCase() === me.email.toLowerCase();
}
