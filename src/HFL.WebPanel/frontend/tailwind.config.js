/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        dark: {
          950: '#07090e',
          900: '#0b0f19',
          850: '#0f172a',
          800: '#131d2f',
          700: '#1e293b',
          600: '#334155',
        },
        neon: {
          blue: '#38bdf8',
          cyan: '#06b6d4',
          purple: '#c084fc',
          pink: '#f43f5e',
          green: '#4ade80',
          amber: '#fbbf24',
        }
      },
      fontFamily: {
        mono: ['"JetBrains Mono"', 'monospace'],
        sans: ['Inter', 'sans-serif'],
      },
      boxShadow: {
        'neon-blue': '0 0 25px rgba(56, 189, 248, 0.25)',
        'neon-purple': '0 0 25px rgba(192, 132, 252, 0.25)',
        'neon-green': '0 0 25px rgba(74, 222, 128, 0.25)',
      },
      backdropBlur: {
        'xs': '2px',
      }
    },
  },
  plugins: [],
}
