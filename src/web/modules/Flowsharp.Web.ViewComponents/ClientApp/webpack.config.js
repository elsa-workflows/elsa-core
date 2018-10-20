'use strict';
const webpack = require('webpack');

module.exports = {
    mode: 'development',
    entry: { main: './src/js/index.js' },
    output: {
        path: __dirname + '/../wwwroot',
        publicPath: '/'
    },
    resolve: {
        modules: [ 'src', 'node_modules' ]
    },
    module: {
        rules: [ { test: /\.jsx?$/, use: 'babel-loader', exclude: /node_modules/ } ]
    },
    plugins: [
        new webpack.HotModuleReplacementPlugin()
    ]
};