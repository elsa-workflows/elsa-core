const defaultTheme = require('tailwindcss/defaultTheme');

module.exports = {
    purge: ["./src/**/*.html"],
    theme: {
        extend: {
            fontFamily: {
                sans: ['Inter var', ...defaultTheme.fontFamily.sans],
            },
        },
        inset: {
            '0': 0,
            auto: 'auto',
            'inverse-right-10': '-1rem',
            'inverse-right-30': '-3rem'
        }
    },
    variants: {
        borderWidth: ['responsive', 'hover', 'focus'],
    },
    plugins: [
        require('@tailwindcss/ui')({
            layout: 'sidebar',
        })
    ],
};
