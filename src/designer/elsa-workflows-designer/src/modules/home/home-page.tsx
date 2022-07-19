import {Component, getAssetPath, h} from "@stencil/core";
import toolbarComponentStore from "../../data/toolbar-component-store";

@Component({
  tag: 'elsa-home-page',
  assetsDirs: ['assets'],
  styleUrl: 'home-page.css',
  shadow: false
})
export class HomePage {

  componentWillLoad(){
    toolbarComponentStore.components = [() => <elsa-new-button/>];
  }

  render() {
    const imageUrl = getAssetPath('./assets/elsa-anim.gif');
    return (
      <div class="default-background h-full">
        <div class="flex max-w-5xl mx-auto">
          <div class="flex-grow">
            <div class="ml-10 lg:py-24">
              <h1 class="mt-4 text-4xl tracking-tight font-extrabold sm:mt-5 sm:text-6xl lg:mt-6 xl:text-6xl">
                <span class="block main-title-color">Elsa Workflows</span>
                <span class="pb-3 block sm:pb-5 sub-title-color">3.0</span>
              </h1>
              <p class="text-base text-gray-800 sm:text-xl lg:text-lg xl:text-xl">
                Decoding the future.
              </p>
            </div>
          </div>
          <div class="flex-shrink">
            <img src={imageUrl} alt="Elsa Workflows - Decoding the future" width={750}/>
          </div>
        </div>
      </div>
    );
  }
}
