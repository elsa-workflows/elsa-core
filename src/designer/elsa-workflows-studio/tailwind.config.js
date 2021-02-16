const defaultTheme = require('tailwindcss/defaultTheme');

module.exports = {
  theme: {
    purge:{
      enabled: true,
      content: ['./src/**/*.tsx', './src/**/*.html'],
      options: {
        safelist: ['jtk-connector']
      },
    },
    extend: {
      fontFamily: {
        sans: ['Inter var', ...defaultTheme.fontFamily.sans],
      },
    }
  },
  variants: {
    borderColor: ['responsive', 'hover', 'focus']
  },
  plugins: [
    require('@tailwindcss/ui')({
      layout: 'sidebar',
    }),
    require('@tailwindcss/forms')
  ],
};
