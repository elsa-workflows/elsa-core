### How to use the OAuth2 Client credentials flow
1. Create a secret with type `OAuth2 credentials`
2. Set the Grant type to `Client Credentials`
3. Fill in the rest of the fields
4. Use the secret as you would use any other authorization secret with the `Send HTTP Request` activity

Now before making the HTTP request, it gets the access token from authorization provider, or use the saved one, if it has not expired.

### How to use the OAuth2 Authorization code flow
1. Create a secret with type `OAuth2 credentials`
2. Set the Grant type to `Authorization Code`
3. Fill in the rest of the fields
4. Click `Authorize`
   1. A window will open, that will take you to the authorization provider url, which is the URL you inserted into `Authorization URL` field
   2. In the authorization provider URL, you will do the steps, that the provider requires you to
   3. The provider will redirect the window back to our callback url
   4. With the callback, we receive the `refresh token` and the initial `access token`
   5. We attach it to the secret, so it would get used with http requests
5. Use the secret as you would use any other authorization secret with the `Send HTTP Request` activity
6. If the refresh token should expire (the activity will fail with the corresponding error), open the secret and repeat step 4