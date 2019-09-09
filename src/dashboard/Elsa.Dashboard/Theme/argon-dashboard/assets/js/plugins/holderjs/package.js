Package.describe({
  summary: 'Holder uses SVG to render image placeholders entirely in browser.',
  version: '2.9.4',
  name: 'imsky:holder',
  git: 'https://github.com/imsky/holder',
});

Package.onUse(function(api) {
  api.versionsFrom('0.9.0');
  api.export('Holder', 'client');
  api.addFiles('holder.js', 'client');
});