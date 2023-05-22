import {Component, h, State, Event, EventEmitter} from "@stencil/core";
import {LoginApi} from "../services";
import {Container} from "typedi";
import {AuthContext} from "../../../services";
import {SignedInArgs} from "../models";

@Component({
  tag: 'elsa-login-page',
  shadow: false
})
export class LoginPage {

  private loginApi: LoginApi;

  @Event() signedIn: EventEmitter<SignedInArgs>;
  @State() showError: boolean;

  constructor() {
    this.loginApi = Container.get(LoginApi);
  }

  private onSubmit = async (e: Event) => {
    e.preventDefault();
    this.showError = false;

    window.requestAnimationFrame(() => {
    });

    const form: HTMLFormElement = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const username = formData.get('username') as string;
    const password = formData.get('password') as string;
    const rememberMe = formData.get('remember-me') as string == 'true';
    const loginResponse = await this.loginApi.login(username, password);

    if (!loginResponse.isAuthenticated) {
      this.showError = true;
    }

    this.signedIn.emit();

    const accessToken = loginResponse.accessToken;
    const refreshToken = loginResponse.refreshToken;
    const authContext = Container.get(AuthContext);
    await authContext.signin(accessToken, refreshToken, rememberMe);
  }

  private renderError = () => {
    if (!this.showError)
      return;

    return <div class="tw-rounded-md tw-bg-red-50 tw-p-4">
      <div class="tw-flex">
        <div class="tw-flex-shrink-0">
          <svg class="tw-h-5 tw-w-5 tw-text-red-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z"
                  clip-rule="evenodd"/>
          </svg>
        </div>
        <div class="tw-ml-3">
          <h3 class="tw-text-sm tw-font-medium tw-text-red-800">Invalid credentials</h3>
          <div class="tw-mt-2 tw-text-sm tw-text-red-700">
            The provided credentials were invalid. Please try again.
          </div>
        </div>
      </div>
    </div>
  }

  render() {
    return <div>
      <div class="tw-flex min-tw-h-full tw-flex-col tw-pt-20 tw-bg-gray-800 tw-h-screen">

        <div class="tw-mt-8 sm:tw-mx-auto sm:tw-w-full sm:tw-max-w-md">
          <div class="tw-bg-white tw-py-8 tw-px-4 tw-shadow sm:tw-rounded-lg sm:tw-px-10">

            {this.renderError()}

            <form class="tw-mt-4 tw-space-y-6" action="#" method="POST" onSubmit={this.onSubmit}>
              <div>
                <label htmlFor="username" class="tw-block tw-text-sm tw-font-medium tw-text-gray-700">Username</label>
                <div class="tw-mt-1">
                  <input id="username" name="username" type="text" autocomplete="off" required
                         class="tw-block tw-w-full tw-appearance-none tw-rounded-md tw-border tw-border-gray-300 tw-px-3 tw-py-2 tw-placeholder-gray-400 tw-shadow-sm focus:tw-border-blue-500 focus:tw-outline-none focus:tw-ring-blue-500 sm:tw-text-sm"/>
                </div>
              </div>

              <div>
                <label htmlFor="password" class="tw-block tw-text-sm tw-font-medium tw-text-gray-700">Password</label>
                <div class="tw-mt-1">
                  <input id="password" name="password" type="password" autoComplete="current-password" required
                         class="tw-block tw-w-full tw-appearance-none tw-rounded-md tw-border tw-border-gray-300 tw-px-3 tw-py-2 tw-placeholder-gray-400 tw-shadow-sm focus:tw-border-blue-500 focus:tw-outline-none focus:tw-ring-blue-500 sm:tw-text-sm"/>
                </div>
              </div>

              <div class="tw-flex tw-items-center tw-justify-between">
                <div class="tw-flex tw-items-center">
                  <input id="remember-me" name="remember-me" type="checkbox" value="true" class="tw-h-4 tw-w-4 tw-rounded tw-border-gray-300 tw-text-blue-600 focus:tw-ring-blue-500"/>
                  <label htmlFor="remember-me" class="tw-ml-2 tw-block tw-text-sm tw-text-gray-900">Remember me</label>
                </div>
              </div>

              <div>
                <button type="submit"
                        class="tw-flex tw-w-full tw-justify-center tw-rounded-md tw-border tw-border-transparent tw-bg-blue-600 tw-py-2 tw-px-4 tw-text-sm tw-font-medium tw-text-white tw-shadow-sm hover:tw-bg-blue-700 focus:tw-outline-none focus:tw-ring-2 focus:tw-ring-blue-500 focus:tw-ring-offset-2">Sign in
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>;
  }
}
