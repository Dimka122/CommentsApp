import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import type { ApiComment } from './types';

/** Подписка на новые комментарии. null в колбэке = соединение восстановилось, возможно, были пропущены события. */
export function useCommentsHub(onEvent: (c: ApiComment | null) => void) {
  const cb = useRef(onEvent);
  useEffect(() => { cb.current = onEvent; }, [onEvent]);

  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/comments')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    conn.on('commentCreated', (c: ApiComment) => cb.current(c));
    conn.onreconnected(() => cb.current(null));
    conn.start().catch(() => { /* сервер недоступен: страница работает и без live-обновлений */ });
    return () => { void conn.stop(); };
  }, []);
}
