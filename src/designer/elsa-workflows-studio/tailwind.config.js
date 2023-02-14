const defaultTheme = require('tailwindcss/defaultTheme');
const colors = require('tailwindcss/colors')

const dev = process.env.NODE_ENV === 'development';

module.exports = {
  //important: 'elsa-studio-root',
  content: ['./src/**/*.tsx', './src/**/*.html'],
  safelist: ['hidden', 'jtk-connector', 'jtk-endpoint', 'x6-node', 'x6-port-out', 'x6-port-label', 'x6-graph-scroller', 'rose', 'sky', 'label-container', 'node', 'start', 'activity', 'border-blue-600', 'border-green-600', 'border-red-600', {
    pattern: /gray|pink|blue|green|red|yellow|rose/
  }],
  prefix: 'elsa-',
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter var', ...defaultTheme.fontFamily.sans],
      },
    }
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
    require('@tailwindcss/aspect-ratio'),
  ],
};
