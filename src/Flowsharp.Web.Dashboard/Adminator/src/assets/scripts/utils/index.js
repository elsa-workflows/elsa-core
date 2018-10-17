import * as $ from 'jquery';

export default (function () {
  // ------------------------------------------------------
  // @Window Resize
  // ------------------------------------------------------

  /**
   * NOTE: Register resize event for Masonry layout
   */
  const EVENT = document.createEvent('UIEvents');
  window.EVENT = EVENT;
  EVENT.initUIEvent('resize', true, false, window, 0);


  window.addEventListener('load', () => {
    /**
     * Trigger window resize event after page load
     * for recalculation of masonry layout.
     */
    window.dispatchEvent(EVENT);
  });

  // ------------------------------------------------------
  // @External Links
  // ------------------------------------------------------

  // Open external links in new window
  $('a')
    .filter('[href^="http"], [href^="//"]')
    .not(`[href*="${window.location.host}"]`)
    .attr('rel', 'noopener noreferrer')
    .attr('target', '_blank');

  // ------------------------------------------------------
  // @Resize Trigger
  // ------------------------------------------------------

  // Trigger resize on any element click
  document.addEventListener('click', () => {
    window.dispatchEvent(window.EVENT);
  });
}());
