const defaultTheme = require('tailwindcss/defaultTheme');

module.exports = {
    purge: ['./**/*.razor', './**/*.cshtml', './**/*.html'],
    theme: {
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
