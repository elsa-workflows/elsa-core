const defaultTheme = require('tailwindcss/defaultTheme');
const colors = require('tailwindcss/colors')

const dev = process.env.NODE_ENV === 'development';

module.exports = {
  purge: {
    enabled: !dev,
    content: ['./src/**/*.tsx', './src/**/*.html'],
    options: {
      safelist: ['hidden', 'jtk-connector', 'rose', 'sky', /gray/, /pink/, /blue/, /green/, /red/, /yellow/, /rose/, 'label-container', 'node', 'start', 'activity']
    },
  },
  prefix: 'elsa-',
  theme: {
    extend: {
      colors: {
        'sky': colors.sky,
        'cool-gray': colors.coolGray,
        teal: colors.teal,
        cyan: colors.cyan,
        rose: colors.rose,
      },
      fontFamily: {
        sans: ['Inter var', ...defaultTheme.fontFamily.sans],
      },
    }
  },
  variants: {
    extend: {
      opacity: ['disabled'],
      cursor: ['disabled'],
    },
    borderColor: ['responsive', 'hover', 'focus'],    
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
    require('@tailwindcss/aspect-ratio'),
  ],
};
