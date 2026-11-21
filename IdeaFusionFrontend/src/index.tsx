import { createRoot } from "react-dom/client";
import App from "./App";
import reportWebVitals from "./reportWebVitals";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ChatProvider } from "./context/ChatContext";
import { DirectChatProvider } from "./context/DirectChatContext";
import { initKeycloak } from "./auth/keycloak";
import { DialogProvider, ToastProvider } from "./ui";
import { ChatPanelProvider } from "./components/ChatWidget";

import "./global.css";

const container = document.getElementById("root") as Element;

// The authorization code comes back from Keycloak in the query string
// (`/?code=...&state=...`). `<Navigate replace>` on "/" rewrites the URL during the
// first render, which drops those parameters before keycloak-js can redeem them: the
// session cookie exists, yet no token is ever stored and every API call 401s. So the
// code is redeemed before the router ever renders. AuthProvider calls initKeycloak()
// too, which is idempotent.
container.textContent = "";
const splash = document.createElement("div");
splash.style.cssText = "min-height:100vh;display:flex;align-items:center;justify-content:center;gap:12px;background:#0e0e11;color:#8a8a99;font:14px ui-monospace,Menlo,Consolas,monospace";
splash.textContent = "Завантаження…";
container.appendChild(splash);

const root = createRoot(container);

initKeycloak()
  .catch(() => {
    // A rejected init means no usable session (logged out, or the code could not be
    // redeemed). AuthProvider reports the state; the app still has to render so the
    // login page and its error message are reachable.
  })
  .finally(() => {
    root.render(
      <BrowserRouter>
        <ToastProvider>
          <DialogProvider>
            <AuthProvider>
              <ChatProvider>
                <DirectChatProvider>
                  <ChatPanelProvider>
                    <App />
                  </ChatPanelProvider>
                </DirectChatProvider>
              </ChatProvider>
            </AuthProvider>
          </DialogProvider>
        </ToastProvider>
      </BrowserRouter>
    );
  });

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
