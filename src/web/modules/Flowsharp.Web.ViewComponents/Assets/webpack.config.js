'use strict';
const webpack = require('webpack');

module.exports = {
    mode: 'development',
    entry: { main: './js/index.js' },
    output: {
        path: __dirname + '/../wwwroot',
        publicPath: '/'
    },
    resolve: {
        modules: [ 'src', 'node_modules' ]
    },
    module: {
        rules: [ 
            { test: /\.js?$/, use: 'babel-loader', exclude: /node_modules/ },
            {
                test: /\.scss$/,
                exclude: /node_modules/,
                use: [
                    {
                        loader: "style-loader" // creates style nodes from JS strings
                    },
                    {
                        loader: "css-loader" // translates CSS into CommonJS
                    },
                    {
                        loader: "sass-loader" // compiles Sass to CSS
                    }
                ]
            }]
    },
    plugins: [
        new webpack.HotModuleReplacementPlugin()
    ]
};