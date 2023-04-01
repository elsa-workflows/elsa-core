const defaultTheme = require('tailwindcss/defaultTheme');
const colors = require('tailwindcss/colors');

// @ts-ignore
const dev = process.argv && process.argv.indexOf('--dev') > -1;

module.exports = {
  content: ['./src/**/*.tsx', './src/**/*.ts'],
  darkMode: 'media',
  important: 'elsa-shell',
  theme: {
    extend: {
      colors: {
        'sky': colors.sky,
        'gray': colors.gray,
        teal: colors.teal,
        cyan: colors.cyan,
        rose: colors.rose,
      },
      fontFamily: {
        sans: ['Inter var', ...defaultTheme.fontFamily.sans],
      },
    },
  },
  variants: {
    extend: {},
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/aspect-ratio')
  ],
}
