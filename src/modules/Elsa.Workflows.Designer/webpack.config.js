const path = require('path');
const CopyPlugin = require("copy-webpack-plugin");

module.exports = env => {

    return {
        entry: ['./index.js'],
        devtool: env && env.production ? 'none' : 'source-map',
        resolve: {
            extensions: ['.js'],
        },
        experiments: {
            outputModule: true
        },
        output: {
            filename: 'elsa-workflows-designer.js',
            path: path.resolve(__dirname, 'wwwroot'),
            libraryTarget: 'module'
        },
        plugins: [
            new CopyPlugin({
                patterns: [
                    { from: "node_modules/@elsa-workflows/elsa-workflows-designer/dist/elsa-workflows-designer", to: "elsa-workflows-designer" },
                    { from: "node_modules/monaco-editor/min", to: "monaco-editor/min" },
                    { from: "node_modules/monaco-editor/min-maps", to: "monaco-editor/min-maps" },
                ],
            }),
        ],
    };
};