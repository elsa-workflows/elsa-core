// ------------------
// @Table of Contents
// ------------------

/**
 * + @Loading Dependencies
 * + @Common Loaders
 * + @Exporting Module
 */


// ---------------------
// @Loading Dependencies
// ---------------------

const
  manifest          = require('../manifest'),
  path              = require('path'),
  cssNext           = require('postcss-cssnext');


// ---------------
// @Common Loaders
// ---------------

const loaders = [
  {
    loader: 'css-loader',
    options: {
      sourceMap : manifest.IS_DEVELOPMENT,
      minimize  : manifest.IS_PRODUCTION,
    },
  },
  {
    loader: 'postcss-loader',
    options: {
      sourceMap: manifest.IS_DEVELOPMENT,
      plugins: () => [
        cssNext(),
      ],
    },
  },
  {
    loader: 'sass-loader',
    options: {
      sourceMap: manifest.IS_DEVELOPMENT,
      includePaths: [
        path.join('../../', 'node_modules'),
        path.join(manifest.paths.src, 'assets', 'styles'),
        path.join(manifest.paths.src, ''),
      ],
    },
  },
];

const rule = {
  test: /\.scss$/,
  use: [{
    loader: 'style-loader',
  }].concat(loaders),
};

// -----------------
// @Exporting Module
// -----------------

module.exports = rule;
