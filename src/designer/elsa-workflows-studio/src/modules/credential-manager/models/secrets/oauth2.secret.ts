import { Secret } from '../secret.model';

export const OAuth2: Secret = {
  category: 'Http',
  customAttributes: {},
  description: 'OAuth2 credentials',
  displayName: 'OAuth2 credentials',
  inputProperties: [
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: 'Grant Type',
      name: 'GrantType',
      order: 0,
      supportedSyntaxes: ['Literal'],
      type: 'System.String',
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: 'Client Credentials',
            value: 'client_credentials',
          },
          {
            text: 'Authorization Code',
            value: 'authorization_code',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: 'Authorization URL',
      name: 'AuthorizationUrl',
      order: 0,
      supportedSyntaxes: ['Literal'],
      type: 'System.String',
      uiHint: 'single-line',
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: 'Access Token URL',
      name: 'AccessTokenUrl',
      order: 1,
      supportedSyntaxes: ['Literal'],
      type: 'System.String',
      uiHint: 'single-line',
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: 'Client ID',
      name: 'ClientId',
      order: 2,
      supportedSyntaxes: ['Literal'],
      type: 'System.String',
      uiHint: 'single-line',
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: 'Client Secret',
      name: 'ClientSecret',
      order: 3,
      supportedSyntaxes: ['Literal'],
      type: 'System.String',
      uiHint: 'single-line',
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: 'Token endpoint client authentication method',
      name: 'ClientAuthMethod',
      order: 4,
      supportedSyntaxes: ['Literal'],
      type: 'System.String',
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: 'Client secret: Basic',
            value: 'client_secret_basic',
          },
          {
            text: 'Client secret: Post',
            value: 'client_secret_post',
          }
        ]
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: 'Scope',
      name: 'Scope',
      order: 5,
      supportedSyntaxes: ['Literal'],
      type: 'System.String',
      uiHint: 'single-line',
    }
  ],
  type: 'OAuth2',
};
