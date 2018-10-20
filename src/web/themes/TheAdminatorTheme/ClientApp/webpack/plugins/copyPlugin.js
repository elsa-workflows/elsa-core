const
  path              = require('path'),
  manifest          = require('../manifest'),
  CopyWebpackPlugin = require('copy-webpack-plugin');

module.exports = new CopyWebpackPlugin([
  {
    from : path.join(manifest.paths.src, 'assets/static'),
    to   : path.join(manifest.paths.build, 'assets/static'),
  },
]);
