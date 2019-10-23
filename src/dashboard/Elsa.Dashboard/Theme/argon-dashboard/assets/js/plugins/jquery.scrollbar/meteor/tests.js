'use strict';

Tinytest.add('Scrollbar integration', function(test) {

  var div = document.createElement('div');
  div.className = 'scrollbar-inner';
  div.value = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam in.";
  var scrollBar = jQuery('.scrollbar-inner').scrollbar();
  console.log(scrollBar);
  test.isNotNull(scrollBar, 'instantiation OK');
});