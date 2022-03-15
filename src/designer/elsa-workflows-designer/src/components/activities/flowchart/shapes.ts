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

  get activity() {
    return this.store.get<Activity>('activity');
  }

  set activity(value: Activity) {
    this.store.set('activity', value);
  }

  get activityDescriptor() {
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

    this.html = {
      render() {

        return self.createHtml();

      },
      shouldComponentUpdate(node: Cell) {
        return node.hasChanged('text');
      },
    };
  }

  updateSize() {
    const activityDescriptor = this.activityDescriptor as ActivityDescriptor;

    if (!activityDescriptor)
      return;

    const wrapper = document.createElement('div');
    wrapper.className = 'w-full flex items-center pl-10 pr-2 py-2';
    wrapper.innerHTML = this.createHtml();
    document.body.append(wrapper);
    const rect = wrapper.firstElementChild.getBoundingClientRect();
    wrapper.remove();

    const width = rect.width;
    const height = rect.height;
    this.prop({size: {width, height}});
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
  //portMarkup: [Markup.getForeignObjectMarkup()],
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
