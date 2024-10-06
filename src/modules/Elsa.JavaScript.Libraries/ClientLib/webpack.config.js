const webpack = require('webpack');
const path = require('path');
const TerserPlugin = require('terser-webpack-plugin');

module.exports = {
    entry: {
        lodash: './src/lodash.js',
        moment: './src/moment.js'
    },
    output: {
        filename: '[name].js',
        path: path.resolve(__dirname, 'dist'),
        library: '[name]',
        libraryTarget: 'umd'
    },
    plugins: [
        new webpack.BannerPlugin({
            banner: `(function(global) {
                if (typeof self === 'undefined') {
                    global.self = global;
                }
            })(typeof global !== 'undefined' ? global : this);`,
            raw: true
        })
    ],
    optimization: {
        minimize: true,
        minimizer: [new TerserPlugin({
            extractComments: false
        })],
    },
    mode: 'production'
};