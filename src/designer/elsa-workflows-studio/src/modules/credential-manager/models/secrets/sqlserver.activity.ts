import { Secret } from "../secret.model";

export const SqlServer: Secret = {
  category: "Sql",
  customAttributes: {},
  description: "Sql connection string",
  displayName: "MSSQL Server",
  inputProperties: [
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Data Source",
      name: "Data Source",
      hint: "FQDN/IP to Server",
      order: 0,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: "single-line",
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Initial Catalog",
      name: "Initial Catalog",
      hint: "Database name",
      order: 1,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.Int64",
      uiHint: "single-line",
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "User ID",
      name: "User ID",
      hint: "Username for connection",
      order: 2,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.Int64",
      uiHint: "single-line",
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Password",
      name: "Password",
      hint: "Password for connection",
      order: 3,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: "single-line",
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Connection Timeout",
      name: "Connection Timeout",
      hint: "The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.",
      order: 5,
      defaultValue: 15,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.Int64",
      uiHint: "single-line",
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Integrated Security",
      name: "Integrated Security",
      hint: "When false, User ID and Password are specified in the connection. When true, the current Windows account credentials are used for authentication. (Default: False)",
      order: 6,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: '',
            value: null,
          },
          {
            text: 'True',
            value: 'True',
          },
          {
            text: 'False',
            value: 'False',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Persist Security Info",
      name: "Persist Security Info",
      hint: "When set to False (strongly recommended), security-sensitive information, such as the password, is not returned as part of the connection if the connection is open or has ever been in an open state. (Default: False)",
      order: 7,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: '',
            value: null,
          },
          {
            text: 'True',
            value: 'True',
          },
          {
            text: 'False',
            value: 'False',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Pooling",
      name: "Pooling",
      hint: "When the value of this key is set to true, any newly created connection will be added to the pool when closed by the application. In a next attempt to open the same connection, that connection will be drawn from the pool. (Default: True)",
      order: 8,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: '',
            value: null,
          },
          {
            text: 'True',
            value: 'True',
          },
          {
            text: 'False',
            value: 'False',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Trust Server Certificate",
      name: "TrustServerCertificate",
      hint: "When set to true, SSL is used to encrypt the channel when bypassing walking the certificate chain to validate trust. If Trust Server Certificate is set to true and Encrypt is set to false, the channel is not encrypted. (Default: False)",
      order: 9,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: '',
            value: null,
          },
          {
            text: 'True',
            value: 'True',
          },
          {
            text: 'False',
            value: 'False',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Encrypt",
      name: "Encrypt",
      hint: "When true, SQL Server uses SSL encryption for all data sent between the client and server if the server has a certificate installed. (Default: False)",
      order: 10,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: '',
            value: null,
          },
          {
            text: 'True',
            value: 'True',
          },
          {
            text: 'False',
            value: 'False',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Multiple Active Result Sets",
      name: "MultipleActiveResultSets",
      hint: "When true, an application can maintain multiple active result sets. When false, an application must process or cancel all result sets from one batch before it can execute any other batch on that connection. (Default: False)",
      order: 11,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: '',
            value: null,
          },
          {
            text: 'True',
            value: 'True',
          },
          {
            text: 'False',
            value: 'False',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Application Name",
      name: "Application Name",
      hint: "The name of the application",
      order: 12,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: "single-line",
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Application Intent",
      name: "ApplicationIntent",
      hint: "Declares the application workload type when connecting to a server. (Default: ReadWrite)",
      order: 13,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: '',
            value: null,
          },
          {
            text: 'Read/Write',
            value: 'ReadWrite',
          },
          {
            text: 'Read Only',
            value: 'ReadOnly',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Asynchronous Processing",
      name: "Async",
      hint: "Enables asynchronous operation support. (Default: False)",
      order: 14,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: '',
            value: null,
          },
          {
            text: 'True',
            value: 'True',
          },
          {
            text: 'False',
            value: 'False',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Load Balance Timeout",
      name: "Load Balance Timeout",
      hint: "When a connection is returned to the pool, its creation time is compared with the current time, and the connection is destroyed if that time span (in seconds) exceeds the value specified by Load Balance Timeout. (Default: 0)",
      order: 5,
      defaultValue: 0,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.Int64",
      uiHint: "single-line",
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Connect Retry Count",
      name: "ConnectRetryCount",
      hint: "Controls the number of reconnection attempts after the client identifies an idle connection failure. (Default: 1)",
      order: 5,
      defaultValue: 1,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.Int64",
      uiHint: "single-line",
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Connect Retry Interval",
      name: "ConnectRetryInterval",
      hint: "Specifies the time between each connection retry attempt. Valid values are 1 to 60 seconds (Default: 10).",
      order: 5,
      defaultValue: 10,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.Int64",
      uiHint: "single-line",
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Multi Subnet Failover",
      name: "MultiSubnetFailover",
      hint: "Multi Subnet Failover configures SqlClient to provide faster detection of and connection to the (currently) active server. (Default: False)",
      order: 14,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: 'dropdown',
      options: {
        isFlagsEnum: false,
        items: [
          {
            text: '',
            value: null,
          },
          {
            text: 'True',
            value: 'True',
          },
          {
            text: 'False',
            value: 'False',
          }
        ],
      }
    },
    {
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isReadOnly: false,
      label: "Additional Settings",
      name: "AdditionalSettings",
      hint: "The content entered will be appended to the end of the generated connection string.",
      order: 20,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: "single-line",
    },
  ],
  type: "MSSQLServer"
}
