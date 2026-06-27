import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import keycloak from './auth/keycloak';
import App from './App';

keycloak
  .init({
    onLoad: 'login-required',
    pkceMethod: 'S256',
    checkLoginIframe: false,
  })
  .then((authenticated) => {
    if (!authenticated) {
      keycloak.login();
      return;
    }
    createRoot(document.getElementById('root')!).render(
      <StrictMode>
        <App />
      </StrictMode>,
    );
  })
  .catch(() => {
    document.getElementById('root')!.innerHTML =
      '<div style="display:flex;align-items:center;justify-content:center;height:100vh;font-family:sans-serif;color:#555">Failed to connect to authentication server. Please try again.</div>';
  });
