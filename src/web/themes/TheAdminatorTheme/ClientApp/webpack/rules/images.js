module.exports = {
  test    : /\.(png|gif|jpg|svg)$/i,
  exclude : /(node_modules)/,
  use     : [{
    loader: 'file-loader',
    options: {
      outputPath: 'assets',
    },
  }],
};
