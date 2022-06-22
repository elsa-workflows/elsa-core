import {Cell, Graph, Shape} from '@antv/x6';
import {Container} from 'typedi';
import {Activity, ActivityDescriptor} from '../../../models';
import {ActivityDriverRegistry, ActivityDisplayContext} from '../../../services';

export class ActivityNode extends Shape.HTML {
  get text() {
    return this.store.get<string>('text');
  }

  set text(value: string) {
    this.store.set('text', value);
  }

  get activity(): Activity {
    return this.store.get<Activity>('activity');
  }

  set activity(value: Activity) {
    this.store.set('activity', value);
  }

  get activityDescriptor(): ActivityDescriptor {
    return this.store.get<ActivityDescriptor>('activityDescriptor');
  }

  set activityDescriptor(value: ActivityDescriptor) {
    this.store.set('activityDescriptor', value);
  }

  init() {
    super.init();
    this.updateSize();
  }

  setup() {
    const self = this;
    super.setup();
    this.on('change:text', this.updateSize, this);
    this.on('change:activity', this.updateSize, this);

    this.html = {
      render() {

        return self.createHtml();

      },
      shouldComponentUpdate(node: Cell) {
        return node.hasChanged('text') || node.hasChanged('activity');
      },
    };
  }

  updateSize() {
    const activityDescriptor = this.activityDescriptor as ActivityDescriptor;

    if (!activityDescriptor)
      return;

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
    const activityDescriptor = this.activityDescriptor as ActivityDescriptor;
    const activity = this.activity as Activity;
    const activityType = activityDescriptor.activityType;
    const driverRegistry = Container.get(ActivityDriverRegistry);
    const driver = driverRegistry.createDriver(activityType);

    const displayContext: ActivityDisplayContext = {
      activity: activity,
      activityDescriptor: activityDescriptor,
      displayType: "designer"
    };

    return driver.display(displayContext);
  }
}

ActivityNode.config({
  ports: {
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
});

Graph.registerNode('activity', ActivityNode, true);
