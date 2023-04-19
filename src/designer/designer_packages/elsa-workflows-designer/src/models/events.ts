export const EventTypes = {
  Studio: {
    Initializing: 'studio:initializing',
  },
  HttpClient: {
    ConfigCreated: 'http-client:config-created',
    ClientCreated: 'http-client:created',
    Unauthorized: 'http-client:unauthorized'
  },
  Auth: {
    SignedIn: 'auth:signed-in',
    SignedOut: 'auth:signed-out',
  },
  Descriptors: {
    Updated: 'descriptors:updated',
  },
  Labels: {
    Updated: 'labels:updated'
  }
};
