import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: { port: 5173, strictPort: true },
  optimizeDeps: {
    include: ['react', 'react-dom', 'react-router-dom', 'axios', '@mui/icons-material'],
  },
})
