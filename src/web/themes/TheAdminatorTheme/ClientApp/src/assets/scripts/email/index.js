import * as $ from 'jquery';

export default (function () {
  $('.email-side-toggle').on('click', e => {
    $('.email-app').toggleClass('side-active');
    e.preventDefault();
  });

  $('.email-list-item, .back-to-mailbox').on('click', e => {
    $('.email-content').toggleClass('open');
    e.preventDefault();
  });
}())
