const defaultTheme = require('tailwindcss/defaultTheme');
const colors = require('tailwindcss/colors')

const dev = process.env.NODE_ENV == 'development';

module.exports = {
  purge: {
    enabled: !dev,
    content: ['./src/**/*.tsx', './src/**/*.html'],
    options: {
      safelist: ['jtk-connector', 'rose', 'light-blue', 'bg-gray-600', 'bg-pink-600', 'bg-blue-600', 'bg-green-600', 'bg-red-600', 'bg-yellow-600']
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
