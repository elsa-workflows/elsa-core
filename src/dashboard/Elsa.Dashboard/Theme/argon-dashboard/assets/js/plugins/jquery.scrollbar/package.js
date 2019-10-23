// package metadata file for Meteor.js
'use strict';

var packageName = 'gromo:jquery.scrollbar'; // https://atmospherejs.com/mediatainment/switchery
var where = 'client'; // where to install: 'client' or 'server'. For both, pass nothing.

Package.describe({
  name: packageName,
  version: '0.2.11',
  // Brief, one-line summary of the package.
  summary: 'Cross-browser CSS customizable scrollbar with advanced features.',
  // URL to the Git repository containing the source code for this package.
  git: 'git@github.com:gromo/jquery.scrollbar.git'
});

Package.onUse(function(api) {
  api.versionsFrom(['METEOR@0.9.0', 'METEOR@1.0']);
  api.use('jquery', where);
  api.addFiles(['jquery.scrollbar.js', 'jquery.scrollbar.css'], where);
});

Package.onTest(function(api) {
  api.use([packageName, 'sanjo:jasmine'], where);
  api.use(['webapp', 'tinytest'], where);
  api.addFiles('meteor/tests.js', where); // testing specific files
});