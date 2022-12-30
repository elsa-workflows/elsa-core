import {Component, h} from '@stencil/core';

@Component({
  tag: 'elsa-oauth2-authorized',
  shadow: false,
})
export class ElsaOauth2Authorized{
  componentDidLoad() {
    window.opener.postMessage("refreshSecrets", "*");
    window.close();
  }

  render() {
    return <div>
      Retrieved consent successfully.
    </div>;
  }
}
