import { defineModule } from "@elsa-workflows/react-studio-sdk";
import { Application } from "./application";
export const shellModule = defineModule({ id: "shell", name: "Shell", core: true,
 extensions: { components: [{id:"application",name:"Application",component:Application}] } });
