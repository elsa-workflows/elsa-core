import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const port = Number(process.env.EXTERNAL_AUTH_FIXTURE_PORT || 4178);
const directory = dirname(fileURLToPath(import.meta.url));
const browserHelperPath = resolve(
  directory,
  '../../../src/modules/Elsa.Studio.ExternalAuthentication.BlazorWasm/wwwroot/external-authentication.js');

const layout = body => `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width">
<title>External Authentication fixture</title>
<style>
body { color: #1f2937; background: #fff; font: 16px/1.5 system-ui, sans-serif; margin: 2rem; }
main { max-width: 64rem; }
label { display: block; margin-top: .75rem; }
input, select, button { font: inherit; margin: .25rem .5rem .5rem 0; padding: .5rem; }
button:focus-visible, input:focus-visible, select:focus-visible { outline: 3px solid #005fcc; outline-offset: 2px; }
table { border-collapse: collapse; width: 100%; }
th, td { border: 1px solid #6b7280; padding: .5rem; text-align: left; }
.external-methods { display: flex; flex-direction: column; align-items: flex-start; margin-top: 1rem; }
.page-heading { align-items: center; display: flex; flex-wrap: wrap; justify-content: space-between; }
.filters { display: flex; flex-wrap: wrap; align-items: end; gap: .75rem; }
.filters label { min-width: 14rem; }
.row-actions { position: relative; }
.row-actions [role="menu"] { background: #fff; border: 1px solid #6b7280; display: grid; position: absolute; right: 0; z-index: 1; }
.row-actions [role="menu"][hidden] { display: none; }
dialog { border: 1px solid #6b7280; border-radius: .5rem; box-shadow: 0 .5rem 2rem rgb(0 0 0 / .25); box-sizing: border-box; color: inherit; max-width: calc(100vw - 2rem); padding: 1.25rem; width: min(38rem, calc(100vw - 2rem)); }
dialog::backdrop { background: rgb(0 0 0 / .45); }
.dialog-header { align-items: start; display: flex; justify-content: space-between; gap: 1rem; }
.dialog-header h2 { margin: 0; }
.dialog-actions { display: flex; flex-wrap: wrap; gap: .5rem; justify-content: flex-end; margin-top: 1rem; }
.warning { border-left: .25rem solid #9a6700; padding-left: .75rem; }
@media (max-width: 40rem) { body { margin: 1rem; } .filters label { min-width: 100%; } dialog { max-width: calc(100vw - 1rem); width: calc(100vw - 1rem); } }
</style>
</head>
<body>${body}<script src="/external-authentication.js"></script></body>
</html>`;

const applicationScript = `
<script>
const key = 'elsa.external-authentication.tokens';
const renderLogin = () => {
  document.body.innerHTML = \`
    <main aria-labelledby="external-login-heading">
      <h1 id="external-login-heading">Sign in</h1>
      <p role="status">Choose an Elsa account or an enabled identity provider.</p>
      <section aria-labelledby="local-login-heading">
        <h2 id="local-login-heading">Elsa account</h2>
        <form id="local-login">
          <label for="username">User name</label>
          <input id="username" name="username" autocomplete="username">
          <label for="password">Password</label>
          <input id="password" name="password" type="password" autocomplete="current-password">
          <button type="submit">Sign in</button>
        </form>
      </section>
      <section class="external-methods" aria-label="External identity providers">
        <div><span role="status">Preferred</span><button type="button" data-external aria-label="Sign in with GitHub"><span aria-hidden="true">github</span><span>Sign in with GitHub</span></button></div>
        <button type="button" data-external aria-label="Sign in with Microsoft"><span aria-hidden="true">microsoft</span><span>Sign in with Microsoft</span></button>
        <button type="button" data-external aria-label="Sign in with Contoso"><span aria-hidden="true">identity provider</span><span>Sign in with Contoso</span></button>
      </section>
    </main>\`;
  document.querySelector('#local-login').onsubmit = event => {
    event.preventDefault();
    location.assign('/workflows');
  };
  document.querySelectorAll('[data-external]').forEach(button => button.onclick = async () => {
    const pkce = await window.elsaExternalAuthentication.createPkce();
    sessionStorage.setItem('elsa.external-authentication.pkce', JSON.stringify(pkce));
    const callback = location.origin + '/authentication/external/callback';
    location.assign('/authorize?response_type=code&client_id=studio-wasm&redirect_uri=' +
      encodeURIComponent(callback) + '&code_challenge=' + encodeURIComponent(pkce.codeChallenge) +
      '&code_challenge_method=S256&state=' + encodeURIComponent(pkce.state));
  });
};
const renderConnections = () => {
  document.body.innerHTML = \`
    <main aria-labelledby="connections-heading">
      <h1 id="connections-heading">Identity provider connections</h1>
      <p role="status">Configuration-owned connections are read-only. Database connections can be managed here.</p>
      <label for="connection-search">Search connections</label>
      <input id="connection-search" type="search">
      <label for="connection-source">Source</label>
      <select id="connection-source"><option>All</option><option>Database</option><option>Configuration</option></select>
      <label><input type="checkbox"> Include archived</label>
      <button type="button">Create connection</button>
      <table aria-label="Identity provider connections">
        <thead><tr><th>Connection</th><th>Source</th><th>Status</th><th>Latest test</th><th aria-label="Actions"></th></tr></thead>
        <tbody><tr><td>Contoso<br><small>contoso · openid-connect</small></td><td>Database</td><td>Enabled, valid</td><td>Not tested</td><td><button type="button">Manage Contoso</button></td></tr></tbody>
      </table>
    </main>\`;
};
const renderIdentityLinks = () => {
  document.body.innerHTML = \`
    <main aria-labelledby="identity-links-heading">
      <div class="page-heading"><h1 id="identity-links-heading">External identity links</h1><button id="create-link" type="button">Create link</button></div>
      <p role="alert">Links are tenant-scoped. Elsa stores a keyed subject hash; raw external subjects and provider tokens are never returned.</p>
      <section aria-label="External identity link filters" class="filters">
        <label for="user-filter">Filter by user ID<input id="user-filter"></label>
        <label for="connection-filter">Filter by connection key<input id="connection-filter"></label>
        <button type="button">Apply filters</button>
      </section>
      <table aria-label="External identity links">
        <thead><tr><th>User</th><th>Connection</th><th>External identity</th><th>Activity</th><th aria-label="Actions"></th></tr></thead>
        <tbody><tr><td>Ada Lovelace</td><td>Contoso</td><td>https://login.contoso.example<br><small>Subject hash · …8f42</small></td><td>Last sign-in 2026-07-25</td><td><div class="row-actions"><button id="ada-link-actions" type="button" aria-label="Actions for Ada Lovelace via Contoso" aria-haspopup="menu" aria-expanded="false">⋮</button><div id="ada-link-menu" role="menu" hidden><button id="edit-ada-link" type="button" role="menuitem">Edit</button><button id="unlink-ada-link" type="button" role="menuitem">Unlink</button></div></div></td></tr></tbody>
      </table>
      <nav aria-label="External identity links pagination"><button type="button" disabled>Previous page</button><span aria-current="page">Page 1</span><button type="button">Next page</button></nav>
    </main>\`;

  const dialog = document.createElement('dialog');
  dialog.id = 'identity-link-dialog';
  dialog.setAttribute('aria-labelledby', 'identity-link-dialog-heading');
  dialog.innerHTML = \`
    <form id="identity-link-form" method="dialog">
      <div class="dialog-header">
        <h2 id="identity-link-dialog-heading"></h2>
        <button id="close-link-dialog" type="button" aria-label="Close link dialog">×</button>
      </div>
      <p id="link-dialog-description"></p>
      <p id="replace-link-warning" class="warning" role="alert" hidden>Replacing this link creates a new external identity link and resets its sign-in history. This cannot be undone.</p>
      <label for="find-link-user">Find Elsa user</label><input id="find-link-user" type="search" autocomplete="off">
      <label for="link-user">Elsa user</label><select id="link-user" required><option value="ada">Ada Lovelace</option><option value="grace">Grace Hopper</option></select>
      <label for="link-connection">Identity provider connection</label><select id="link-connection" required><option value="contoso">Contoso</option><option value="github">GitHub</option></select>
      <label for="link-issuer">Issuer namespace</label><input id="link-issuer" type="url" required>
      <label for="link-subject">External subject</label><input id="link-subject" type="password" required autocomplete="off" aria-describedby="link-subject-help">
      <p id="link-subject-help">Accepted for this operation only; it will not be returned.</p>
      <button id="toggle-link-subject" type="button" aria-label="Show external subject">Show subject</button>
      <div class="dialog-actions"><button id="cancel-link-dialog" type="button">Cancel</button><button id="save-link" type="submit"></button></div>
    </form>\`;
  document.body.append(dialog);

  let invokingControl;
  let editing = false;
  const form = dialog.querySelector('#identity-link-form');
  const heading = dialog.querySelector('#identity-link-dialog-heading');
  const description = dialog.querySelector('#link-dialog-description');
  const warning = dialog.querySelector('#replace-link-warning');
  const save = dialog.querySelector('#save-link');
  const subject = dialog.querySelector('#link-subject');
  const toggleSubject = dialog.querySelector('#toggle-link-subject');
  const actionsTrigger = document.querySelector('#ada-link-actions');
  const actionsMenu = document.querySelector('#ada-link-menu');
  const resetDialog = () => {
    form.reset();
    subject.type = 'password';
    toggleSubject.textContent = 'Show subject';
    toggleSubject.setAttribute('aria-label', 'Show external subject');
    warning.hidden = true;
    editing = false;
  };
  const closeDialog = () => {
    if (dialog.open) dialog.close();
    resetDialog();
    invokingControl?.focus();
  };
  const openDialog = (mode, invoker) => {
    resetDialog();
    invokingControl = invoker;
    editing = mode === 'edit';
    heading.textContent = editing ? 'Edit external identity link' : 'Create external identity link';
    description.textContent = editing ? 'Update this external identity link by replacing it with a new link.' : 'Enter the external identity details to create a link.';
    save.textContent = editing ? 'Replace link' : 'Create link';
    warning.hidden = !editing;
    if (editing) {
      dialog.querySelector('#link-user').value = 'ada';
      dialog.querySelector('#link-connection').value = 'contoso';
      dialog.querySelector('#link-issuer').value = 'https://login.contoso.example';
    }
    dialog.showModal();
    dialog.querySelector('#find-link-user').focus();
  };
  document.querySelector('#create-link').onclick = event => openDialog('create', event.currentTarget);
  actionsTrigger.onclick = () => {
    actionsMenu.hidden = false;
    actionsTrigger.setAttribute('aria-expanded', 'true');
    actionsMenu.querySelector('[role="menuitem"]').focus();
  };
  document.querySelector('#edit-ada-link').onclick = () => {
    actionsMenu.hidden = true;
    actionsTrigger.setAttribute('aria-expanded', 'false');
    openDialog('edit', actionsTrigger);
  };
  document.querySelector('#unlink-ada-link').onclick = event => {
    actionsMenu.hidden = true;
    actionsTrigger.setAttribute('aria-expanded', 'false');
    const confirmation = document.createElement('dialog');
    confirmation.setAttribute('aria-labelledby', 'unlink-dialog-heading');
    confirmation.innerHTML = '<form method="dialog"><h2 id="unlink-dialog-heading">Unlink external identity?</h2><p>The user will no longer sign in through this external identity.</p><div class="dialog-actions"><button value="cancel">Cancel</button><button value="unlink">Unlink</button></div></form>';
    document.body.append(confirmation);
    confirmation.onclose = () => { confirmation.remove(); actionsTrigger.focus(); };
    confirmation.showModal();
  };
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && !actionsMenu.hidden) {
      actionsMenu.hidden = true;
      actionsTrigger.setAttribute('aria-expanded', 'false');
      actionsTrigger.focus();
    }
  });
  dialog.querySelector('#close-link-dialog').onclick = closeDialog;
  dialog.querySelector('#cancel-link-dialog').onclick = closeDialog;
  dialog.addEventListener('cancel', () => window.setTimeout(resetDialog));
  toggleSubject.onclick = () => {
    const showing = subject.type === 'text';
    subject.type = showing ? 'password' : 'text';
    toggleSubject.textContent = showing ? 'Show subject' : 'Hide subject';
    toggleSubject.setAttribute('aria-label', showing ? 'Show external subject' : 'Hide external subject');
  };
  form.addEventListener('submit', event => {
    event.preventDefault();
    const message = editing ? 'External identity link replaced. Sign-in history has been reset.' : 'External identity link created.';
    closeDialog();
    const status = document.createElement('p');
    status.setAttribute('role', 'status');
    status.textContent = message;
    document.querySelector('main').prepend(status);
  });
};
const renderWorkflows = (warning, logoutMode) => {
  document.body.innerHTML = '<main><h1>Workflows</h1>' +
    (warning ? '<p role="status">' + warning + '</p>' : '') +
    (logoutMode ? '<button id="logout">Sign out ' + logoutMode + '</button>' : '') + '</main>';
  const logout = document.querySelector('#logout');
  if (logout) logout.onclick = () => {
    sessionStorage.removeItem(key);
    localStorage.removeItem(key);
    location.assign(logoutMode === 'upstream' ? '/logout/continue/one-time' : '/login');
  };
};
const tokenFor = storage => JSON.stringify({ accessToken: 'access', refreshToken: 'refresh-rotated' });
const signIn = params => {
  const storage = params.get('storage') || 'Memory';
  const logoutMode = params.get('logout');
  if (storage === 'Session') sessionStorage.setItem(key, tokenFor(storage));
  if (storage === 'Durable') localStorage.setItem(key, tokenFor(storage));
  sessionStorage.setItem('fixture.storage', storage);
  if (logoutMode) sessionStorage.setItem('fixture.logout', logoutMode);
  history.replaceState({}, '', '/workflows');
  renderWorkflows(
    storage === 'Session' ? 'Security warning: credentials use browser session storage.' :
    storage === 'Durable' ? 'Security warning: credentials use persistent browser storage.' : '',
    logoutMode);
};
const restore = () => {
  const storage = sessionStorage.getItem('fixture.storage');
  const authenticated = storage === 'Session' ? sessionStorage.getItem(key) :
    storage === 'Durable' ? localStorage.getItem(key) : null;
  if (!authenticated) return location.replace('/login');
  renderWorkflows(
    storage === 'Session' ? 'Security warning: credentials use browser session storage.' :
    'Security warning: credentials use persistent browser storage.',
    sessionStorage.getItem('fixture.logout'));
};
const path = location.pathname;
if (path === '/login') renderLogin();
else if (path === '/security/external-authentication/connections') renderConnections();
else if (path === '/security/external-authentication/identity-links') renderIdentityLinks();
else if (path === '/__external-authentication-fixture/sign-in') signIn(new URLSearchParams(location.search));
else if (path === '/__external-authentication-fixture/reuse-rotated-refresh-token') {
  sessionStorage.removeItem(key); localStorage.removeItem(key); location.replace('/login');
}
else if (path === '/__external-authentication-fixture/callback-replay') {
  sessionStorage.removeItem('elsa.external-authentication.pkce'); location.replace('/login?choose=true');
}
else if (path === '/workflows' || path === '/') restore();
</script>`;

createServer(async (request, response) => {
  const url = new URL(request.url, `http://127.0.0.1:${port}`);
  if (url.pathname === '/external-authentication.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end(await readFile(browserHelperPath));
    return;
  }
  if (url.pathname === '/logout/continue/one-time') {
    response.writeHead(302, { location: '/login' });
    response.end();
    return;
  }
  response.writeHead(200, {
    'content-type': 'text/html; charset=utf-8',
    'cache-control': 'no-store',
    'content-security-policy': "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'"
  });
  response.end(layout(applicationScript));
}).listen(port, '127.0.0.1', () => {
  process.stdout.write(`External Authentication browser fixture listening on http://127.0.0.1:${port}\n`);
});
