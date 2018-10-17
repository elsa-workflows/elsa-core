module.exports = {
  test: /\.(eot|svg|ttf|woff|woff2)$/,
  exclude : /(node_modules)/,
  use     : ['file-loader'],
};
