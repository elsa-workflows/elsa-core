const path = require('path');

module.exports = env => {

    return {
        entry: ['./Interop/ElsaElements.tsx'],
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
                    // use: {
                    //     loader: 'babel-loader',
                    //     options: {
                    //         presets: [
                    //             [
                    //                 "@babel/preset-env",
                    //                 {
                    //                     targets: {
                    //                         esmodules: true
                    //                     }
                    //                 }
                    //             ]
                    //         ]
                    //     }
                    //},
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
            filename: 'elsa-studio.js',
            path: path.resolve(__dirname, 'wwwroot'),
            //module: true,
            //iife: false,
            //library: 'MyLib',
            libraryTarget: 'module',
            //scriptType: 'module'
        },
    };
};