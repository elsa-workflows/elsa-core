import {Component, h, Listen, Method, Prop} from '@stencil/core';
import {RouterHistory} from "@stencil/router";
import {leave, toggle} from 'el-transition'
import { t } from 'i18next';
import { createElsaClient } from '../../../services';
import { DropdownButtonItem, DropdownButtonOrigin } from '../elsa-dropdown-button/models';
import Tunnel from "../../../data/dashboard";
import {AuthenticationConfguration,UserDetail} from "../../../services/elsa-client";
import { StringLiteral } from 'typescript';


@Component({
  tag: 'elsa-user-context-menu',
  shadow: false,
})
export class ElsaUserContextMenu {
 @Prop({attribute: 'serverUrl', reflect: true}) serverUrl: string;
 userDetail :UserDetail = null;
 authenticationConfguration : AuthenticationConfguration;
 async componentWillRender()
 {
  try{
    this.userDetail = await (await (await createElsaClient(this.serverUrl)).authenticationApi.getUserDetails());
    this.authenticationConfguration = await (await (await createElsaClient(this.serverUrl)).authenticationApi.getAuthenticationConfguration());
  }catch(err){
    this.userDetail = null;
  }
 }

 logoutStrategy = {
  "OpenIdConnect" : function(){
    window.location.href = 'v1/ElsaAuthentication/logout';
  },
  "ServerManagedCookie" : function(){
    window.location.href = 'v1/ElsaAuthentication/logout';
  },
  "JwtBearerToken":""
 };



@Method()
 async menuItemSelected(item : DropdownButtonItem)
 {
  if(item.value == 'logout')
  {
    this.authenticationConfguration.authenticationStyles.forEach(x=>{
      this.logoutStrategy[x]();
    });
  }
 }
  render() {
    if(this.userDetail == null)
    {
      return  ('')
    }
    const ddlitems: Array<DropdownButtonItem> = [{'text':("logout") , value : "logout" }].map(x => {
      const item: DropdownButtonItem = {text: x.text, isSelected: false, value: x.value};
      return item
    }); // this dropdown only used for logout for now

   return (<elsa-dropdown-button text={this.userDetail.name} items={ddlitems} btnClass='elsa-bg-gray-800 elsa-text-gray-300 elsa-w-full   elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-inline-flex elsa-justify-center elsa-text-sm elsa-font-medium'
  origin={DropdownButtonOrigin.TopRight}
  onItemSelected={e =>  this.menuItemSelected(e.detail)} />)
}
}
Tunnel.injectProps(ElsaUserContextMenu, ['serverUrl']);

