import { Component, h, Prop } from '@stencil/core';

@Component({
  tag: 'elsa-workflow-definition-editor-toolbar',
})
export class Toolbar {
  @Prop()
  public zoomToFit: () => Promise<void>;

  @Prop()
  public autoLayout: () => Promise<void>;

  render() {
    return (
      <div class="elsa-panel-toolbar flex justify-center absolute border-b border-gray-200 top-0 px-1 pl-4 pb-2 text-sm bg-white z-10 space-x-2">
        <button class="btn btn-primary" onClick={this.autoLayout}>
          Auto-Layout
        </button>
        <button onClick={this.zoomToFit}class="btn btn-primary">
          Zoom to fit
        </button>
      </div>
    );
  }
}
