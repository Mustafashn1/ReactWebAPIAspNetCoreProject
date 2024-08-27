import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000, // Client port
    proxy: {
      '/pizzas': {
        target: 'http://localhost:5100', // Server port
        changeOrigin: true,
        secure: false, // HTTPS kullanmıyorsanız uygundur
        ws: true, // WebSocket kullanıyorsanız uygundur
        configure: (proxy) => {
          proxy.on('error', (err) => {
            console.log('Proxy error:', err);
          });
          proxy.on('proxyReq', (proxyReq, req) => {
            console.log('Sending request to target:', req.method, req.url);
          });
          proxy.on('proxyRes', (proxyRes, req) => {
            console.log('Received response from target:', proxyRes.statusCode, req.url);
          });
        },
      }
    }
  }
})
