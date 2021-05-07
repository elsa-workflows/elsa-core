import {Component, h, Prop, getAssetPath} from '@stencil/core';

@Component({
  tag: 'elsa-studio-home',
  shadow: false,
  assetsDirs: ['assets']
})
export class ElsaStudioHome {

  render() {
    const visualPath = getAssetPath('./assets/undraw_breaking_barriers_vnf3.svg');
    
    return (
      <div class="relative bg-gray-800 overflow-hidden h-screen">
      <main class="mt-16 sm:mt-24">
        <div class="mx-auto max-w-7xl">
          <div class="lg:grid lg:grid-cols-12 lg:gap-8">
            <div class="px-4 sm:px-6 sm:text-center md:max-w-2xl md:mx-auto lg:col-span-6 lg:text-left lg:flex lg:items-center">
              <div>
                <h1 class="mt-4 text-4xl tracking-tight font-extrabold text-white sm:mt-5 sm:leading-none lg:mt-6 lg:text-5xl xl:text-6xl">
                  <span class="md:block">Welcome to</span>
                  
                  <span class="text-teal-400 md:block">Elsa Workflows</span>
                </h1>
                <p class="mt-3 text-base text-gray-300 sm:mt-5 sm:text-xl lg:text-lg xl:text-xl">
                  Use the dashboard app to manage all the things.
                </p>
              </div>
            </div>
            <div class="mt-16 sm:mt-24 lg:mt-0 lg:col-span-6">
              <div class="sm:max-w-md sm:w-full sm:mx-auto sm:rounded-lg sm:overflow-hidden">
                <div class="px-4 py-8 sm:px-10">
                  <img src={visualPath} alt="" width={400} />
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
