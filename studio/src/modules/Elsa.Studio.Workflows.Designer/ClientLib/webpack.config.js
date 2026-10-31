const path = require('path');
const CopyPlugin = require('copy-webpack-plugin');

module.exports = {
    entry: {
        designer: './src/designer.ts',
        'react-designer': './src/react-designer.ts'
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
    plugins: [
        new CopyPlugin({
            patterns: [
                {from: 'css/designer.v1.css', to: './'},
                {from: 'css/designer.v2.css', to: './'},
                {from: 'css/designer.bpmn.css', to: './'},
                {from: 'css/designer.v2.css', to: './designer.css'}, // Default
            ],
        }),
    ]
};