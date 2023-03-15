import 'reflect-metadata';
import {Container, Service} from "typedi";
import cookies from 'js-cookie';
import authStore from "../data/auth-store";
import {EventBus} from "./event-bus";
import {EventTypes} from "../models";
import jwt_decode from "jwt-decode";

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
      authStore.refreshToken = authData.refreshToken;
    }
  }

  async signin(accessToken: string, refreshToken: string, createPersistentCookie: boolean){
    await this.updateTokens(accessToken, refreshToken, createPersistentCookie);
    await this.eventBus.emit(EventTypes.Auth.SignedIn)
  }

  async updateTokens(accessToken: string, refreshToken: string, createPersistentCookie: boolean){
    const claims = jwt_decode<any>(accessToken);
    const permissions = claims.permissions || [];
    const name = claims.name || '';
    await this.updateSession(name, permissions, accessToken, refreshToken, createPersistentCookie);
  }

  updateSession(name: string, permissions: Array<string>, accessToken: string, refreshToken: string, createPersistentCookie: boolean) {
    authStore.name = name;
    authStore.permissions = permissions;
    authStore.signedIn = true;
    authStore.accessToken = accessToken;
    authStore.refreshToken = refreshToken;

    const data = JSON.stringify(authStore);
    sessionStorage.setItem('dashboard-session', data);

    if (createPersistentCookie) {
      cookies.set("dashboard-session", data);
    }
  }

  async signOut() {
    authStore.name = null;
    authStore.permissions = [];
    authStore.signedIn = false;
    authStore.accessToken = null;
    authStore.refreshToken = null;
    sessionStorage.clear();
    cookies.remove('dashboard-session');
    await this.eventBus.emit(EventTypes.Auth.SignedOut)
  }

  getIsSignedIn() {
    return authStore.signedIn;
  }

  getAccessToken() {
    return authStore.accessToken;
  }

  getRefreshToken() {
    return authStore.refreshToken;
  }
}
