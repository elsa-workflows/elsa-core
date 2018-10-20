// ------------------
// @Table of Contents
// ------------------

/**
 * + @Loading Dependencies
 * + @Common Plugins
 * + @Merging Production Plugins
 * + @Merging Development Plugins
 * + @Exporting Module
 */


// ---------------------
// @Loading Dependencies
// ---------------------

const
  manifest = require('../manifest'),
  webpack  = require('webpack');


// ---------------
// @Common Plugins
// ---------------

const
  plugins  = [];

plugins.push(
  new webpack.DefinePlugin({
    'process.env': {
      NODE_ENV: JSON.stringify(manifest.NODE_ENV),
    },
  }),

  new webpack.optimize.CommonsChunkPlugin({
    name: 'vendor',
    filename: manifest.outputFiles.vendor,
    minChunks(module) {
      const { context } = module;
      return context && context.indexOf('node_modules') >= 0;
    },
  }),

  new webpack.ProvidePlugin({
    $: 'jquery',
    jQuery: 'jquery',
    'window.jQuery': 'jquery',
    Popper: ['popper.js', 'default'],
  })
);


// ---------------------------
// @Merging Production Plugins
// ---------------------------

if (manifest.IS_PRODUCTION) {
  plugins.push(
    new webpack.optimize.UglifyJsPlugin({
      compress: {
        comparisons   : true,
        conditionals  : true,
        dead_code     : true,
        drop_debugger : true,
        evaluate      : true,
        if_return     : true,
        join_vars     : true,
        screw_ie8     : true,
        sequences     : true,
        unused        : true,
        warnings      : false,
      },

      output: {
        comments: false,
      },
    })
  );
}


// ----------------------------
// @Merging Development Plugins
// ----------------------------

if (manifest.IS_DEVELOPMENT) {
  plugins.push(
    new webpack.NoEmitOnErrorsPlugin(),
    new webpack.NamedModulesPlugin(),
    new webpack.HotModuleReplacementPlugin()
  );
}


// -----------------
// @Exporting Module
// -----------------

module.exports = plugins;
