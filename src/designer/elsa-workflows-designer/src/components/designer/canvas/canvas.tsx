import {Component, h, Method, Prop} from '@stencil/core';
import {ContainerActivityComponent} from '../../activities/container-activity-component';
import {Activity, ActivityDescriptor} from '../../../models';

export interface AddActivityArgs {
  descriptor: ActivityDescriptor;
  x?: number;
  y?: number;
}

@Component({
  tag: 'elsa-canvas',
  styleUrl: 'canvas.scss',
})
export class Canvas {

  private root: ContainerActivityComponent;

  @Prop() public interactiveMode: boolean = true;

  @Method()
  public async addActivity(args: AddActivityArgs): Promise<void> {
    await this.root.addActivity(args);
  }

  @Method()
  public async updateLayout(): Promise<void> {
    await this.root.updateLayout();
  }

  @Method()
  public async exportGraph(): Promise<Activity> {
    return await this.root.exportRoot();
  }

  @Method()
  public async importGraph(root: Activity): Promise<void> {
    return await this.root.importRoot(root);
  }

  render() {
    return (
      <div class="absolute left-0 top-0 right-0 bottom-0">
        <elsa-flowchart ref={el => this.root = el} interactiveMode={this.interactiveMode} class="absolute left-0 top-0 right-0 bottom-0"/>
      </div>
    );
  }
}
