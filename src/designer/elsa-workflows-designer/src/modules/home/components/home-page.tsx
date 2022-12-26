import 'reflect-metadata';
import {Component, getAssetPath, h} from "@stencil/core";
import toolbarComponentStore from "../../../data/toolbar-component-store";

@Component({
  tag: 'elsa-home-page',
  assetsDirs: ['assets'],
  styleUrl: 'home-page.css',
  shadow: false
})
export class HomePage {

  async componentWillLoad(){
    toolbarComponentStore.components = [() => <elsa-new-button/>];
  }

  render() {
    const imageUrl = getAssetPath('./assets/elsa-anim.gif');
    const visualPath = getAssetPath('./assets/elsa-breaking-barriers-undraw.svg');
    return (
      <div class="home-wrapper relative bg-gray-800 overflow-hidden h-screen">
        <main class="mt-16 sm:mt-24">
          <div class="mx-auto max-w-7xl">
            <div class="lg:grid lg:grid-cols-12 lg:gap-8">
              <div class="px-4 sm:px-6 sm:text-center md:max-w-2xl md:mx-auto lg:col-span-6 lg:text-left lg:flex lg:items-center">
                <div class="home-caption-wrapper">
                  <h1 class="mt-4 text-4xl tracking-tight font-extrabold text-white sm:mt-5 sm:leading-none lg:mt-6 lg:text-5xl xl:text-6xl">
                    <span class="md:block"> Welcome to <span class='text-blue-500 md:block'>Elsa Workflows</span> <span>3.0</span></span>
                  </h1>
                  <p class="tagline mt-3 text-base text-gray-300 sm:mt-5 sm:text-xl lg:text-lg xl:text-xl">
                    Decoding the future.
                  </p>
                </div>
              </div>
              <div class="mt-16 sm:mt-24 lg:mt-0 lg:col-span-6">
                <div class="sm:max-w-md sm:w-full sm:mx-auto sm:rounded-lg sm:overflow-hidden">
                  <div class="px-4 py-8 sm:px-10">
                    <img class="home-visual" src={visualPath} alt="" width={400}/>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </main>
      </div>
    );
  }
}
