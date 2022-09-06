import 'reflect-metadata';
import {Container, Service} from "typedi";
import cookies from 'js-cookie';
import authStore from "../data/auth-store";
import {EventBus} from "./event-bus";
import {EventTypes} from "../models";

export const AuthEventTypes = {
  Unauthorized: 'auth:unauthorized'
}

@Service()
export class AuthContext {
  private eventBus: EventBus;

  constructor(eventBus: EventBus) {
    this.eventBus = eventBus;
    const sessionData = sessionStorage.getItem('dashboard-session');
    const data = sessionData || cookies.get('dashboard-session');

    if (data && data.length > 0) {
      const authData = JSON.parse(data);
      authStore.name = authData.name;
      authStore.permissions = authData.permissions;
      authStore.signedIn = authData.signedIn;
      authStore.accessToken = authData.accessToken;
    }
  }

  async signIn(name: string, permissions: Array<string>, accessToken: string, createPersistentCookie: boolean) {
    authStore.name = name;
    authStore.permissions = permissions;
    authStore.signedIn = true;
    authStore.accessToken = accessToken;

    const data = JSON.stringify(authStore);
    sessionStorage.setItem('dashboard-session', data);

    if (createPersistentCookie) {
      cookies.set("dashboard-session", data);
    }

    await this.eventBus.emit(EventTypes.Auth.SignedIn)
  }

  async signOut() {
    authStore.name = null;
    authStore.permissions = [];
    authStore.signedIn = false;
    authStore.accessToken = false;
    await this.eventBus.emit(EventTypes.Auth.SignedOut)
  }

  getIsSignedIn() {
    return authStore.signedIn;
  }

  getAccessToken() {
    return authStore.accessToken;
  }
}
