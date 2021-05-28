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
      <div class="elsa-relative elsa-bg-gray-800 elsa-overflow-hidden elsa-h-screen">
      <main class="elsa-mt-16 sm:elsa-mt-24">
        <div class="elsa-mx-auto elsa-max-w-7xl">
          <div class="lg:elsa-grid lg:elsa-grid-cols-12 lg:elsa-gap-8">
            <div class="elsa-px-4 sm:elsa-px-6 sm:elsa-text-center md:elsa-max-w-2xl md:elsa-mx-auto lg:elsa-col-span-6 lg:elsa-text-left lg:flex lg:elsa-items-center">
              <div>
                <h1 class="elsa-mt-4 elsa-text-4xl elsa-tracking-tight elsa-font-extrabold elsa-text-white sm:elsa-mt-5 sm:elsa-leading-none lg:elsa-mt-6 lg:elsa-text-5xl xl:elsa-text-6xl">
                  <span class="md:elsa-block">Welcome to</span>
                  
                  <span class="elsa-text-teal-400 md:elsa-block">Elsa Workflows</span> <span>2.0</span>
                </h1>
                <p class="elsa-mt-3 elsa-text-base elsa-text-gray-300 sm:elsa-mt-5 sm:elsa-text-xl lg:elsa-text-lg xl:elsa-text-xl">
                  Use the dashboard app to manage all the things.
                </p>
              </div>
            </div>
            <div class="elsa-mt-16 sm:elsa-mt-24 lg:elsa-mt-0 lg:elsa-col-span-6">
              <div class="sm:elsa-max-w-md sm:elsa-w-full sm:elsa-mx-auto sm:elsa-rounded-lg sm:elsa-overflow-hidden">
                <div class="elsa-px-4 elsa-py-8 sm:elsa-px-10">
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
