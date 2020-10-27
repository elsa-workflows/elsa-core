const defaultTheme = require('tailwindcss/defaultTheme');

module.exports = {
  purge: ["./public/**/*.html"],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter var', ...defaultTheme.fontFamily.sans],
      },
    },
  },
  variants: {},
  plugins: [
    require('@tailwindcss/ui')({
      layout: 'sidebar',
    })
  ],
};
