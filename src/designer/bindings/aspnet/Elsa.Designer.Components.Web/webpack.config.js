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