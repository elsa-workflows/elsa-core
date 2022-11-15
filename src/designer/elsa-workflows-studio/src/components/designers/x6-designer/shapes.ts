import {Cell, Graph, Shape} from '@antv/x6';
import {ActivityModel, ActivityDescriptor, ActivityDesignDisplayContext} from '../../../models';

export class ActivityNodeShape extends Shape.HTML {

  get component() {
    return this.store.get<string>('component');
  }

  set component(value: string) {
    this.store.set('component', value);
  }

  set activity(value: ActivityModel) {
    this.store.set('activity', value);
  }

  get activity(): ActivityModel {
    return this.store.get<ActivityModel>('activity');
  }

  get activityDescriptor(): ActivityDescriptor {
    return this.store.get<ActivityDescriptor>('activityDescriptor');
  }

  set activityDescriptor(value: ActivityDescriptor) {
    this.store.set('activityDescriptor', value);
  }

  get displayContext() {
    return this.store.get<ActivityDesignDisplayContext>('displayContext');
  }

  set displayContext(value: ActivityDesignDisplayContext) {
    this.store.set('displayContext', value);
  }

  init() {
    super.init();
    this.updateSize();
  }

  setup() {
    const self = this;
    super.setup();
    this.on('change:component', this.updateSize, this);
    this.on('change:activity', this.updateSize, this);

    this.html = {
      render() {

        return self.createHtml();

      },
      shouldComponentUpdate(node: Cell) {

        return node.hasChanged('component') || node.hasChanged('activity');
      },
    };
  }

  updateSize() {
    const wrapper = document.createElement('div');
    wrapper.className = 'w-full flex items-center pl-10 pr-2 py-2 absolute';
    wrapper.style.left = '-1000px';
    wrapper.style.top = '-1000px';
    wrapper.innerHTML = this.createHtml();

    document.body.append(wrapper);

    // Wait for activity element to be completely rendered.
    // When using custom elements, they are rendered after they are mounted. Before then, they have a 0 width and height.
    const tryUpdateSize = () => {
      const activityElement: Element = wrapper.getElementsByTagName('elsa-default-activity-template')[0];
      const activityElementRect = activityElement.getBoundingClientRect();

      // If the custom element has no width or height yet, it means it has not yet rendered.
      if (activityElementRect.width == 0 || activityElementRect.height == 0) {
        // Request an animation frame and call ourselves back immediately after.
        window.requestAnimationFrame(tryUpdateSize);
        return;
      }

      const rect = wrapper.firstElementChild.getBoundingClientRect();
      const width = rect.width;
      const height = rect.height;

      // Update size of the activity node.
      this.prop({size: {width, height}});

      // Remove the temporary element (used only to calculate its size).
      wrapper.remove();
    };

    // Begin try to get our element size.
    tryUpdateSize();
  }

  createHtml() {
    const component = this.component;

    return `<elsa-default-activity-template
       class="activity elsa-inline-block elsa-rounded elsa-bg-white elsa-text-left elsa-text-black elsa-text-lg elsa-select-none elsa-max-w-md elsa-shadow-sm elsa-relative">
        ${component}</elsa-default-activity-template>`
  }
}

ActivityNodeShape.config({
  ports: {
    items: [
      {
        group: 'out',
      },
      {
        group: 'in',
      },
    ],
    groups: {
      in: {
        position: 'dynamicIn',
        attrs: {
          circle: {
            r: 6,
            magnet: true,
            stroke: '#3c82f6',
            strokeWidth: 2,
            fill: '#fff',
          },
          text: {
            fontSize: 12,
            fill: '#888',
          },
        },
        label: {
          position: {
            name: 'outside',
          },
        },
      },
      out: {
        position: 'dynamicOut',
        attrs: {
          circle: {
            r: 6,
            magnet: true,
            stroke: '#fff',
            strokeWidth: 2,
            fill: '#3c82f6',
          },
          text: {
            fontSize: 12,
            fill: '#888',
          },
        },
        label: {
          position: {
            name: 'outside',
          },
        },
      },
    },
  },
  portMarkup: [
    {
      tagName: 'circle',
      selector: 'portBody',
    },
  ],
});

Graph.registerNode('activity', ActivityNodeShape, true);
