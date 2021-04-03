const defaultTheme = require('tailwindcss/defaultTheme');
const colors = require('tailwindcss/colors')

const dev = process.env.NODE_ENV == 'development';

module.exports = {
  purge: {
    enabled: !dev,
    content: ['./src/**/*.tsx', './src/**/*.html'],
    options: {
      safelist: ['jtk-connector', 'rose', 'light-blue']
    },
  },
  theme: {
    extend: {
      colors: {
        'light-blue': colors.lightBlue,
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
    borderColor: ['responsive', 'hover', 'focus']
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
    require('@tailwindcss/aspect-ratio'),
  ],
};
