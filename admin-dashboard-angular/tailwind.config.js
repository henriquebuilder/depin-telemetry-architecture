/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts,css,scss,less,sass}",
  ],
  theme: {
    extend: {
      colors: {
        background: '#0d0e12',
        surface: '#121214',
        surfaceLight: '#1a1b1e',
        border: '#2a2b30',
        textPrimary: '#ffffff',
        textSecondary: '#a1a1aa',
        textTertiary: '#71717a',
        success: '#10b981',
        danger: '#ef4444',
        warning: '#f59e0b',
      },
      fontFamily: {
        sans: ['-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      animation: {
        'pulse-slow': 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      },
    },
  },
  plugins: [],
}
