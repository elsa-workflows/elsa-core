import {Component, Host, h, Prop, State, Listen, Method} from '@stencil/core';
import * as monaco from 'monaco-editor';

@Component({
  tag: 'elsa-monaco',
  styleUrl: 'elsa-monaco.css',
  shadow: false,
})
export class ElsaMonaco {

  container: HTMLElement;

  componentDidRender(){
    monaco.editor.create(this.container, {
      value: [
        '1+1'
      ].join('\n'),
      language: 'javascript',
      lineNumbersMinChars: 0,
      overviewRulerLanes: 0,
      overviewRulerBorder: false,
      lineDecorationsWidth: 0,
      hideCursorInOverviewRuler: false,
      cursorBlinking: 'blink',
      glyphMargin: false,
      folding: false,
      scrollBeyondLastColumn: 0,
      scrollbar: {horizontal: 'hidden', vertical: 'hidden'},
      find: {addExtraSpaceOnTop: false, autoFindInSelection: 'never', seedSearchStringFromSelection: false},
      minimap: {enabled: false},
    });
  }

  render() {
    return (
      <Host>
        <div ref={el => this.container = el} class="h-full">
        </div>
      </Host>
    )
  }
}
