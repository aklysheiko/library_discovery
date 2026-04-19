import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: false,
    proxy: {
      // Proxy API calls to the backend running on http://localhost:5248
      '/api': {
        target: 'http://localhost:5248',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
