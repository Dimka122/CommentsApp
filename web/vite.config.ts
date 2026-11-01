import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

// В dev-режиме проксируем backend (docker compose публикует API на 127.0.0.1:8080).
const api = 'http://localhost:8080';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': api,
      '/graphql': api,
      '/hubs': { target: api, ws: true },
    },
  },
  test: { environment: 'node' },
});
