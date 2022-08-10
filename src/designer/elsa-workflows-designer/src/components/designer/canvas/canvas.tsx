import {Component, h, Method, Prop} from '@stencil/core';
import {ContainerActivityComponent} from '../../../services';
import {Activity, ActivityDescriptor} from '../../../models';

export interface AddActivityArgs {
  descriptor: ActivityDescriptor;
  id?: string;
  x?: number;
  y?: number;
}

export interface UpdateActivityArgs {
  id: string;
  originalId: string;
  activity: Activity;
}

export interface RenameActivityArgs {
  originalId: string;
  newId: string;
  activity: Activity;
}

@Component({
  tag: 'elsa-canvas',
  styleUrl: 'canvas.scss',
})
export class Canvas {

  private root: ContainerActivityComponent;

  @Prop() public interactiveMode: boolean = true;

  @Method()
  public async getRootComponent(): Promise<ContainerActivityComponent> {
    return this.root;
  }

  @Method()
  public async addActivity(args: AddActivityArgs): Promise<Activity> {
    console.debug(`Activity type: ${args.descriptor.activityType}`)
    return await this.root.addActivity(args);
  }

  @Method()
  public async updateActivity(args: UpdateActivityArgs): Promise<void> {
    await this.root.updateActivity(args);
  }

  @Method()
  public async renameActivity(args: RenameActivityArgs): Promise<void> {
    await this.root.renameActivity(args);
  }

  @Method()
  public async updateLayout(): Promise<void> {
    await this.root.updateLayout();
  }

  @Method()
  public async exportGraph(): Promise<Activity> {
    return await this.root.export();
  }

  @Method()
  public async zoomToFit(): Promise<void> {
    return await this.root.zoomToFit();
  }

  @Method()
  public async importGraph(root: Activity): Promise<void> {
    return await this.root.import(root);
  }

  @Method()
  async reset(): Promise<void> {
    await this.root.reset();
  }

  @Method()
  async newRoot(): Promise<Activity> {
    return await this.root.newRoot();
  }

  render() {
    return (
      <div class="absolute left-0 top-0 right-0 bottom-0">
        <elsa-flowchart ref={el => this.root = el} interactiveMode={this.interactiveMode} class="absolute left-0 top-0 right-0 bottom-0"/>
      </div>
    );
  }
}
