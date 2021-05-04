const path = require('path');
const CopyPlugin = require("copy-webpack-plugin");

module.exports = env => {

    return {
        entry: ['./Interop/elsa-interop.ts'],
        devtool: env && env.production ? 'none' : 'source-map',
        module: {
            rules: [
                {
                    test: /\.(ts|tsx)$/,
                    use: 'ts-loader',
                    exclude: /node_modules/,
                },
                {
                    test: /\.(js|jsx)$/,
                    exclude: /node_modules/,
                },
            ],
        },
        resolve: {
            extensions: ['.tsx', '.ts', '.js', '.jsx'],
        },
        experiments: {
            outputModule: true
        },
        output: {
            filename: 'elsa-workflows.js',
            path: path.resolve(__dirname, 'wwwroot'),
            libraryTarget: 'module'
        },
        plugins: [
            new CopyPlugin({
                patterns: [
                    { from: "../../../elsa-workflows-studio/dist/elsa-workflows-studio", to: "elsa-workflows-studio" },
                    { from: "node_modules/monaco-editor/min", to: "monaco-editor/min" },
                    { from: "node_modules/monaco-editor/min-maps", to: "monaco-editor/min-maps" },
                ],
            }),
        ],
    };
};