import 'reflect-metadata';
import {Component, getAssetPath, h} from "@stencil/core";
import toolbarComponentStore from "../../../data/toolbar-component-store";
import newButtonItemStore from "../../../data/new-button-item-store";
import {DropdownButtonOrigin} from "../../../components/shared/dropdown-button/models";

@Component({
  tag: 'elsa-home-page',
  assetsDirs: ['assets'],
  styleUrl: 'home-page.css',
  shadow: false
})
export class HomePage {

  async componentWillLoad() {
    const mainItem = newButtonItemStore.mainItem ?? {
      text: 'New'
    };

    toolbarComponentStore.components = [() => <elsa-dropdown-button items={newButtonItemStore.items} text={mainItem.text} handler={mainItem.handler} origin={DropdownButtonOrigin.TopRight}/>];
  }

  render() {
    const visualPath = getAssetPath('./assets/elsa-breaking-barriers-undraw.svg');
    return (
      <div class="home-wrapper tw-relative tw-bg-gray-800 tw-overflow-hidden tw-h-screen">
        <main class="tw-mt-16 sm:tw-mt-24">
          <div class="tw-mx-auto tw-max-w-7xl">
            <div class="lg:tw-grid lg:tw-grid-cols-12 lg:tw-gap-8">
              <div class="tw-px-4 sm:tw-px-6 sm:tw-text-center md:tw-max-w-2xl md:tw-mx-auto lg:tw-col-span-6 lg:tw-text-left lg:tw-flex lg:tw-items-center">
                <div class="home-caption-wrapper">
                  <h1 class="tw-mt-4 tw-text-4xl tw-tracking-tight tw-font-extrabold tw-text-white sm:tw-mt-5 sm:tw-leading-none lg:tw-mt-6 lg:tw-text-5xl xl:tw-text-6xl">
                    <span class="md:tw-block"> Welcome to <span class='tw-text-blue-500 md:tw-block'>Elsa Workflows</span> <span>3.0</span></span>
                  </h1>
                  <p class="tagline tw-mt-3 tw-text-base tw-text-gray-300 sm:tw-mt-5 sm:tw-text-xl lg:tw-text-lg xl:tw-text-xl">
                    Decoding the future.
                  </p>
                </div>
              </div>
              <div class="tw-mt-16 sm:tw-mt-24 lg:tw-mt-0 lg:tw-col-span-6">
                <div class="sm:tw-max-w-md sm:tw-w-full sm:tw-mx-auto sm:tw-rounded-lg sm:tw-overflow-hidden">
                  <div class="tw-px-4 tw-py-8 sm:tw-px-10">
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
