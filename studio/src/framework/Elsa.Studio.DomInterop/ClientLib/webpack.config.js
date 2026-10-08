const path = require('path');

module.exports = {
    entry: {
        dom: './src/dom.ts',
        clipboard: './src/clipboard.ts',
        files: './src/files.ts',
    },
    output: {
        filename: '[name].entry.js',
        path: path.resolve(__dirname, '..', 'wwwroot'),
        library: {
            type: "module",
        },
        module: true, // Enable outputting ES6 modules.
        environment: {
            module: true, // The environment supports ES6 modules.
        },
    },
    experiments: {
        outputModule: true, // This enables the experimental support for outputting ES6 modules
    },
    devtool: 'source-map',
    mode: 'development',
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
            {
                test: /\.css$/,
                use: ['style-loader', 'css-loader'],
            },
            {
                test: /\.(eot|woff(2)?|ttf|otf|svg)$/i,
                type: 'asset'
            },
        ]
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
};