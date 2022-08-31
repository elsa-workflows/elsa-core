import {Component, h, State} from "@stencil/core";
import {LoginApi} from "../services";
import {Container} from "typedi";
import jwt_decode from "jwt-decode";
import {AuthContext} from "../../../services";

@Component({
  tag: 'elsa-login-page',
  shadow: false
})
export class LoginPage {

  private loginApi: LoginApi;

  @State() showError: boolean;

  constructor() {
    this.loginApi = Container.get(LoginApi);
  }

  private onSubmit = async (e: Event) => {
    e.preventDefault();
    this.showError = false;

    window.requestAnimationFrame(() => {});

    const form: HTMLFormElement = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const username = formData.get('username') as string;
    const password = formData.get('password') as string;
    const rememberMe = formData.get('remember-me') as string == 'true';
    const loginResponse = await this.loginApi.login(username, password);

    if (!loginResponse.isAuthenticated) {
      this.showError = true;
    }

    const accessToken = loginResponse.accessToken;
    const claims = jwt_decode<any>(accessToken);
    const permissions = claims.permissions || [];
    const name = claims.name || '';
    const authContext = Container.get(AuthContext);
    await authContext.signIn(name, permissions, accessToken, rememberMe);
  }

  private renderError = () => {
    if(!this.showError)
      return;

    return <div class="rounded-md bg-red-50 p-4">
      <div class="flex">
        <div class="flex-shrink-0">
          <svg class="h-5 w-5 text-red-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z"
                  clip-rule="evenodd"/>
          </svg>
        </div>
        <div class="ml-3">
          <h3 class="text-sm font-medium text-red-800">Invalid credentials</h3>
          <div class="mt-2 text-sm text-red-700">
            The provided credentials were invalid. Please try again.
          </div>
        </div>
      </div>
    </div>
  }

  render() {
    return <div>
      <div class="flex min-h-full flex-col justify-center py-12 sm:px-6 lg:px-8">

        <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
          <div class="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">

            {this.renderError()}

            <form class="mt-4 space-y-6" action="#" method="POST" onSubmit={this.onSubmit}>
              <div>
                <label htmlFor="username" class="block text-sm font-medium text-gray-700">Username</label>
                <div class="mt-1">
                  <input id="username" name="username" type="text" autocomplete="off" required
                         class="block w-full appearance-none rounded-md border border-gray-300 px-3 py-2 placeholder-gray-400 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-blue-500 sm:text-sm"/>
                </div>
              </div>

              <div>
                <label htmlFor="password" class="block text-sm font-medium text-gray-700">Password</label>
                <div class="mt-1">
                  <input id="password" name="password" type="password" autoComplete="current-password" required
                         class="block w-full appearance-none rounded-md border border-gray-300 px-3 py-2 placeholder-gray-400 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-blue-500 sm:text-sm"/>
                </div>
              </div>

              <div class="flex items-center justify-between">
                <div class="flex items-center">
                  <input id="remember-me" name="remember-me" type="checkbox" value="true" class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"/>
                  <label htmlFor="remember-me" class="ml-2 block text-sm text-gray-900">Remember me</label>
                </div>
              </div>

              <div>
                <button type="submit"
                        class="flex w-full justify-center rounded-md border border-transparent bg-blue-600 py-2 px-4 text-sm font-medium text-white shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2">Sign in
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>;
  }
}
