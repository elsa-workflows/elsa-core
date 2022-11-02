import {Cell, Graph, Shape, Edge} from '@antv/x6';
import {ActivityModel, ActivityDescriptor, ActivityDesignDisplayContext} from '../../models';
import {activityIconProvider} from '../../services';

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
    this.store.set('displayContext', value)
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
      render(node: Cell) {

        // console.log("node:", node)

        return self.createHtml();

      },
      shouldComponentUpdate(node: Cell) {

        return node.hasChanged('component') || node.hasChanged('activity');
      },
    };
  }

  updateSize() {
    const activityDescriptor = this.activityDescriptor as ActivityDescriptor;

    // if (!activityDescriptor)
    //   return;

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

  // getUsedOutPorts(graph: Graph) {
  //   const outgoingEdges = graph.getOutgoingEdges(this) || []
  //   return outgoingEdges.map((edge: Edge) => {
  //     const portId = edge.getTargetPortId()
  //     return this.getPort(portId!)
  //   })
  // }

  // getNewOutPorts(length: number) {
  //   return Array.from(
  //     {
  //       length,
  //     },
  //     () => {
  //       return {
  //         group: 'out',
  //       }
  //     },
  //   )
  // }

  // getInPorts() {
  //   return this.getPortsByGroup('in')
  // }

  // getOutPorts() {
  //   return this.getPortsByGroup('out')
  // }

  // updateInPorts(graph: Graph) {
  //   const minNumberOfPorts = 1
  //   const ports = this.getInPorts()
  //   const usedPorts = this.getUsedInPorts(graph)
  //   const newPorts = this.getNewInPorts(
  //     Math.max(minNumberOfPorts - usedPorts.length, 1),
  //   )

  //   console.log("ports:", ports)
  //   console.log("usedPorts:", usedPorts)

  //   if (
  //     ports.length === minNumberOfPorts &&
  //     ports.length - usedPorts.length > 0
  //   ) {
  //     // noop
  //   } else if (ports.length === usedPorts.length) {
  //     this.addPorts(newPorts)
  //   } else if (ports.length + 1 > usedPorts.length) {
  //     this.prop(
  //       ['ports', 'items'],
  //       this.getInPorts().concat(usedPorts).concat(newPorts),
  //       {
  //         rewrite: true,
  //       },
  //     )
  //   }

  //   return this
  // }

  // getUsedInPorts(graph: Graph) {
  //   const incomingEdges = graph.getIncomingEdges(this) || []
  //   return incomingEdges.map((edge: Edge) => {
  //     const portId = edge.getTargetPortId()
  //     return this.getPort(portId!)
  //   })
  // }

  // getNewInPorts(length: number) {
  //   return Array.from(
  //     {
  //       length,
  //     },
  //     () => {
  //       return {
  //         group: 'in',
  //       }
  //     },
  //   )
  // }

  // renderActivityBody(displayContext: ActivityDesignDisplayContext) {
  //   if (displayContext && displayContext.expanded) {
  //     return (
  //       `<div class="elsa-border-t elsa-border-t-solid">
  //         <div class="elsa-p-4 elsa-text-gray-400 elsa-text-sm">
  //           <div class="elsa-mb-2">${!!displayContext?.bodyDisplay ? displayContext.bodyDisplay : ''}</div>
  //           <div>
  //             <span class="elsa-inline-flex elsa-items-center elsa-px-2.5 elsa-py-0.5 elsa-rounded-full elsa-text-xs elsa-font-medium elsa-bg-gray-100 elsa-text-gray-500">
  //               <svg class="-elsa-ml-0.5 elsa-mr-1.5 elsa-h-2 elsa-w-2 elsa-text-gray-400" fill="currentColor" viewBox="0 0 8 8">
  //                 <circle cx="4" cy="4" r="3" />
  //               </svg>
  //               ${displayContext != undefined ? displayContext.activityModel.activityId : ''}
  //             </span>
  //           </div>
  //         </div>
  //     </div>`
  //     );
  //   }

  //   return '';
  // }

  // async componentWillRender() {
  //   if (!!this.activityDisplayContexts)
  //     return;

  //   const activity = this.activity as ActivityModel;

  //     const displayContext = await this.getActivityDisplayContext(activity);

  //   this.activityDisplayContexts = displayContext;
  // }

  // activityDisplayContexts: Map<ActivityDesignDisplayContext> = null;
  // oldActivityDisplayContexts: Map<ActivityDesignDisplayContext> = null;

  // createNotFoundActivityDescriptor(activityModel: ActivityModel): ActivityDescriptor {
  //   return {
  //     outcomes: ['Done'],
  //     inputProperties: [],
  //     type: `(Not Found) ${activityModel.type}`,
  //     outputProperties: [],
  //     displayName: `(Not Found) ${activityModel.displayName || activityModel.name || activityModel.type}`,
  //     traits: ActivityTraits.Action,
  //     description: `(Not Found) ${activityModel.description}`,
  //     category: 'Not Found',
  //     browsable: false,
  //     customAttributes: {}
  //   };
  // }

//   async getActivityDisplayContext(activityModel: ActivityModel): Promise<ActivityDesignDisplayContext> {
//     const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;
//     let descriptor = activityDescriptors.find(x => x.type == activityModel.type);
//     let descriptorExists = !!descriptor;
//     const oldContextData = (this.oldActivityDisplayContexts && this.oldActivityDisplayContexts[activityModel.activityId]) || {expanded: false};
// console.log("descriptor:", descriptor, "descriptorExists:", descriptorExists)
//     if (!descriptorExists)
//       descriptor = this.createNotFoundActivityDescriptor(activityModel);

//     const description = descriptorExists ? activityModel.description : `(Not Found) ${descriptorExists}`;
//     const bodyText = description && description.length > 0 ? description : undefined;
//     const bodyDisplay = bodyText ? `<p>${bodyText}</p>` : undefined;
//     // const color = (descriptor.traits &= ActivityTraits.Trigger) == ActivityTraits.Trigger ? 'rose' : 'sky';
//     const displayName = descriptorExists ? activityModel.displayName : `(Not Found) ${activityModel.displayName}`;

//     const displayContext: ActivityDesignDisplayContext = {
//       activityModel: activityModel,
//       activityDescriptor: descriptor,
//       // activityIcon: <ActivityIcon color={color}/>,
//       activityIcon: "",
//       bodyDisplay: bodyDisplay,
//       displayName: displayName,
//       outcomes: [...activityModel.outcomes],
//       expanded: oldContextData.expanded
//     };

//     await eventBus.emit(EventTypes.ActivityDesignDisplaying, this, displayContext);
//     return displayContext;
//   }

  createHtml() {
    const activityDescriptor = this.activityDescriptor as ActivityDescriptor;
    const activity = this.activity as ActivityModel;
    const activityIcon = activityIconProvider.getIcon(activity.type);

    const component = this.component;

    // activityDisplayContext[activity.activityId] = this.getActivityDisplayContext(activity);
    // const displayContext = this.getActivityDisplayContext(activity);

    return `<elsa-default-activity-template id=${`activity-${activity.activityId}`}
       class="activity elsa-inline-block elsa-border-2 elsa-border-solid elsa-rounded elsa-bg-white elsa-text-left elsa-text-black elsa-text-lg elsa-select-none elsa-max-w-md elsa-shadow-sm elsa-relative">
        ${component}</elsa-default-activity-template>`

    // return `<elsa-default-activity-template id=${`activity-${activity.activityId}`}
    //   class="activity elsa-inline-block elsa-border-2 elsa-border-solid elsa-rounded elsa-bg-white elsa-text-left elsa-text-black elsa-text-lg elsa-select-none elsa-max-w-md elsa-shadow-sm elsa-relative">
    //   <div class="elsa-p-3">
    //     <div class="elsa-flex elsa-justify-between elsa-space-x-4">
    //       <div class="elsa-flex-shrink-0">
    //       ${activityIcon || ''}
    //       </div>

    //       <div class="elsa-flex-1 elsa-font-medium elsa-leading-8 elsa-overflow-hidden">
    //         <p class="elsa-overflow-ellipsis elsa-text-base">${activity.displayName}</p>
    //         ${activity.type !== activity.displayName ? `<p class="elsa-text-gray-400 elsa-text-sm">${activity.type}</p>` : ''}
    //       </div>

    //       <div class="elsa--mt-2">
    //         <div class="context-menu-button-container">
    //         </div>
    //         <button type="button" class="expand elsa-ml-1 elsa-text-gray-400 elsa-rounded-full elsa-bg-transparent hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-text-gray-500 focus:elsa-bg-gray-100 elsa-transition elsa-ease-in-out elsa-duration-150">
    //           <svg xmlns="http://www.w3.org/2000/svg" class="elsa-w-6 elsa-h-6 elsa-text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
    //             <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
    //           </svg>
    //         </button>
    //       </div>
    //     </div>
    //     <div class="elsa-border-t elsa-border-t-solid">
    //       <div class="elsa-p-4 elsa-text-gray-400 elsa-text-sm">

    //         <div>
    //           <span class="elsa-inline-flex elsa-items-center elsa-px-2.5 elsa-py-0.5 elsa-rounded-full elsa-text-xs elsa-font-medium elsa-bg-gray-100 elsa-text-gray-500">
    //             <svg class="-elsa-ml-0.5 elsa-mr-1.5 elsa-h-2 elsa-w-2 elsa-text-gray-400" fill="currentColor" viewBox="0 0 8 8">
    //               <circle cx="4" cy="4" r="3" />
    //             </svg>
    //             ${activity.activityId || ''}
    //           </span>
    //         </div>
    //       </div>
    //     </div>

    //   </div>
    // </elsa-default-activity-template>`
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
