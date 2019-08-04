import { Component, h } from '@stencil/core';

@Component({
  tag: 'app-github-link',
  shadow: false
})
export class AppGitHubLink {

  render() {
    return (
      <a href="https://github.com/elsa-workflows/elsa-core" class="github-link">
        <svg width="70" height="70" viewBox="0 0 250 250" aria-hidden="true">
          <defs>
            <linearGradient id="grad1" x1="0%" y1="75%" x2="100%" y2="0%">
              <stop offset="0%" style={ { 'stop-color': '#896def', 'stop-opacity': '1' } } />
              <stop offset="100%" style={ { 'stop-color': '#482271', 'stop-opacity': '1' } } />
            </linearGradient>
          </defs>
          <path d="M 0,0 L115,115 L115,115 L142,142 L250,250 L250,0 Z" fill="url(#grad1)" />
        </svg>
        <i class="mdi mdi-github-circle" />
      </a>
    );
  }
}
