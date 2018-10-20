module.exports = {
  test    : /\.(js)$/,
  exclude : /(node_modules|wwwroot|build|dist\/)/,
  use     : ['babel-loader'],
};
