const
  manifest          = require('../manifest'),
  ExtractTextPlugin = require('extract-text-webpack-plugin');

module.exports = new ExtractTextPlugin({
  filename: manifest.outputFiles.css,
  allChunks: true,
});
