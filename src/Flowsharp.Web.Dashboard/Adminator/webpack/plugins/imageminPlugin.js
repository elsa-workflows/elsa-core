const
  manifest          = require('../manifest'),
  ImageminPlugin    = require('imagemin-webpack-plugin').default;

module.exports = new ImageminPlugin({
  disable: manifest.IS_DEVELOPMENT,
});
