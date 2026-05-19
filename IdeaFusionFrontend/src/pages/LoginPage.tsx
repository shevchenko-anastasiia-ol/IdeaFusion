import { FunctionComponent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

const s: Record<string, React.CSSProperties> = {
  page:    { minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", background: "#0f0f1a" },
  card:    { background: "#1a1a2e", border: "1px solid #2a2a4a", borderRadius: 16, padding: "40px 36px", width: 380, display: "flex", flexDirection: "column", gap: 20 },
  title:   { color: "#fff", fontSize: 24, fontWeight: 700, marginBottom: 4 },
  tabs:    { display: "flex", gap: 0, borderBottom: "1px solid #2a2a4a", marginBottom: 8 },
  tab:     { flex: 1, padding: "10px 0", background: "none", border: "none", color: "#777", fontSize: 15, cursor: "pointer" },
  tabActive: { color: "#fff", borderBottom: "2px solid #7c5cfc" },
  label:   { color: "#aaa", fontSize: 13, marginBottom: 4 },
  input:   { background: "#111122", border: "1px solid #2a2a4a", borderRadius: 8, color: "#fff", padding: "10px 14px", fontSize: 14, outline: "none", width: "100%", boxSizing: "border-box" as const },
  btn:     { background: "#7c5cfc", color: "#fff", border: "none", borderRadius: 8, padding: "12px 0", fontSize: 15, fontWeight: 600, cursor: "pointer", width: "100%" },
  err:     { color: "#ff6b6b", fontSize: 13 },
  link:    { color: "#7c5cfc", fontSize: 13, textAlign: "center" as const, cursor: "pointer", background: "none", border: "none" },
};

const LoginPage: FunctionComponent = () => {
  const navigate       = useNavigate();
  const { login, register } = useAuth();
  const [tab,    setTab]    = useState<"login" | "register">("login");
  const [email,  setEmail]  = useState('');
  const [name,   setName]   = useState('');
  const [pass,   setPass]   = useState('');
  const [pass2,  setPass2]  = useState('');
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (tab === "register" && pass !== pass2) { setError('Паролі не збігаються'); return; }
    setLoading(true);
    try {
      if (tab === "login") {
        await login({ email, password: pass });
        navigate('/home');
      } else {
        await register({ userName: name, email, password: pass });
        setSuccess(true);
        setTab("login");
      }
    } catch (ex: any) {
      setError(ex.message ?? 'Помилка');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={s.page}>
      <div style={s.card}>
        <div style={s.title}>IdeaFusion</div>

        <div style={s.tabs}>
          <button style={{ ...s.tab, ...(tab === "login"    ? s.tabActive : {}) }} onClick={() => { setTab("login");    setError(null); }}>Увійти</button>
          <button style={{ ...s.tab, ...(tab === "register" ? s.tabActive : {}) }} onClick={() => { setTab("register"); setError(null); }}>Реєстрація</button>
        </div>

        {success && <p style={{ color: "#3fcca0", fontSize: 13 }}>Акаунт створено! Тепер увійдіть.</p>}

        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          {tab === "register" && (
            <div>
              <div style={s.label}>Ім'я користувача</div>
              <input style={s.input} type="text" value={name} onChange={e => setName(e.target.value)} placeholder="username" required />
            </div>
          )}
          <div>
            <div style={s.label}>Email</div>
            <input style={s.input} type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="you@example.com" required />
          </div>
          <div>
            <div style={s.label}>Пароль</div>
            <input style={s.input} type="password" value={pass} onChange={e => setPass(e.target.value)} placeholder="••••••••" required minLength={8} />
          </div>
          {tab === "register" && (
            <div>
              <div style={s.label}>Підтвердіть пароль</div>
              <input style={s.input} type="password" value={pass2} onChange={e => setPass2(e.target.value)} placeholder="••••••••" required />
            </div>
          )}

          {error && <p style={s.err}>{error}</p>}

          <button style={s.btn} type="submit" disabled={loading}>
            {loading ? '...' : tab === "login" ? "Увійти" : "Зареєструватись"}
          </button>
        </form>
      </div>
    </div>
  );
};

export default LoginPage;