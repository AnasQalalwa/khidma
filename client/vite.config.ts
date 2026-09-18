/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
  },
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false,
        configure: (proxy) => {
          proxy.on('proxyRes', (proxyRes) => {
            const cookies = proxyRes.headers['set-cookie']
            if (!cookies) {
              return
            }

            proxyRes.headers['set-cookie'] = cookies.map((cookie) =>
              cookie.replace(/;\s*Secure/gi, ''),
            )
          })
        },
      },
    },
  },
  build: {
    outDir: '../server/Khidma.Api/wwwroot',
    emptyOutDir: true,
  },
})
