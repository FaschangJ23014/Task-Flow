<script lang="ts">
 const API_URL = "http://localhost:5000/api";

 let Username : string = $state("");
 let Password : string = $state("");
 let isAuthenticated = $state(false);

 async function loginUser(username: string, password: string) {
    try {
        const response = await fetch(`${API_URL}/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password })
        });

        if(!response.ok) {
            throw new Error("Login failed");
        }

        const data = await response.json();
        
        if(data.token) {
            localStorage.setItem("token", data.token);
            isAuthenticated = true;
            console.log("User logged in successfully");
        } 
    } catch (error) {
        console.error("Error logging in:", error);
    }
}

 async function registerUser(username: string, password: string) {
    const response = await fetch(`${API_URL}/auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password })
    });
    return await response.json();
}
</script>

<div class="page-wrapper">
    <main class="container">
      {#if !isAuthenticated}
        <div class="auth-box">
            <div class="header-text">
                <h2>Login</h2>
                <p>Willkommen zurück! Bitte Daten eingeben.</p>
            </div>
            
            <div class="input-group">
                <div class="field">
                    <label>Username</label>
                    <input type="text" placeholder="Username" bind:value={Username} />
                </div>
                <div class="field">
                    <label>Password</label>
                    <input type="password" placeholder="Password" bind:value={Password} />
                </div>
            </div>

            <div class="button-group">
                <button class="btn-register" onclick={() => registerUser(Username, Password)}>Registrieren</button>
                <button class="btn-login" onclick={() => loginUser(Username, Password)}>Einloggen</button>
            </div>
        </div>
      {:else}
        <div class="auth-box">
            <div class="header-text">
                <h2>Willkommen, {Username}! ⚡</h2>
                <p>Dein Dashboard wird geladen...</p>
            </div>
            <button class="btn-logout" onclick={() => {
                localStorage.removeItem("token");
                isAuthenticated = false;
            }}>Abmelden</button>
        </div>
      {/if}
    </main>

    <footer class="app-footer">
        <p>© 2026 TaskFlow • Built with Svelte & .NET • Jakob Faschang</p>
        <div class="footer-links">
            <a href="#privacy">Datenschutz</a>
            <a href="#terms">AGB</a>
            <a href="#help">Hilfe</a>
        </div>
    </footer>
</div>

<style>
    /* Der Wrapper sorgt dafür, dass die *gesamte* Seite den dunklen Hintergrund hat */
    .page-wrapper {
        min-height: 100vh;
        background-color: #09090b; /* Dunkles Zink-Schwarz für die ganze Seite */
        color: #f4f4f5;
        display: flex;
        flex-direction: column;
        font-family: system-ui, -apple-system, sans-serif;
    }

    /* Container zentriert die Login-Box und nimmt den verfügbaren Platz ein */
    .container {
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 1rem;
    }

    /* Die Haupt-Card */
    .auth-box {
        width: 100%;
        max-width: 28rem;
        background-color: #18181b; /* Zink-900 */
        border: 1px solid #27272a; /* Zink-800 */
        padding: 2rem;
        border-radius: 1rem;
        box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
    }

    .header-text h2 {
        font-size: 1.5rem;
        font-weight: 700;
        margin: 0 0 0.25rem 0;
        color: #ffffff;
    }

    .header-text p {
        font-size: 0.875rem;
        color: #a1a1aa;
        margin: 0;
    }

    .input-group {
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    .field {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
    }

    .field label {
        font-size: 0.75rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: #a1a1aa;
    }

    .field input {
        width: 100%;
        background-color: #09090b;
        border: 1px solid #27272a;
        border-radius: 0.75rem;
        padding: 0.75rem 1rem;
        font-size: 0.875rem;
        color: #ffffff;
        outline: none;
        box-sizing: border-box;
        transition: border-color 0.2s, ring 0.2s;
    }

    .field input:focus {
        border-color: #059669; /* Smaragdgrün */
        box-shadow: 0 0 0 2px rgba(5, 150, 105, 0.2);
    }

    .button-group {
        display: flex;
        gap: 0.75rem;
        padding-top: 0.5rem;
    }

    button {
        flex: 1;
        padding: 0.75rem 1rem;
        border-radius: 0.75rem;
        font-size: 0.875rem;
        font-weight: 500;
        border: none;
        cursor: pointer;
        transition: background-color 0.2s, transform 0.1s;
    }

    button:active {
        transform: scale(0.98);
    }

    .btn-register {
        background-color: #27272a;
        color: #d4d4d8;
    }

    .btn-register:hover {
        background-color: #3f3f46;
    }

    .btn-login {
        background-color: #059669; /* Smaragdgrün */
        color: #ffffff;
        box-shadow: 0 4px 14px rgba(5, 150, 105, 0.3);
    }

    .btn-login:hover {
        background-color: #047857;
    }

    .btn-logout {
        width: 100%;
        background-color: rgba(239, 68, 68, 0.1);
        color: #f87171;
        border: 1px solid rgba(239, 68, 68, 0.2);
    }

    .btn-logout:hover {
        background-color: rgba(239, 68, 68, 0.2);
    }

    /* Footer Design - jetzt im einheitlichen Dark-Look */
    .app-footer {
        padding: 1.5rem 2rem;
        border-top: 1px solid #18181b;
        background-color: #09090b;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.75rem;
        font-size: 0.8rem;
        color: #71717a;
        text-align: center;
    }

    .footer-links {
        display: flex;
        gap: 1.5rem;
    }

    .footer-links a {
        color: #a1a1aa;
        text-decoration: none;
        transition: color 0.2s;
    }

    .footer-links a:hover {
        color: #059669;
    }

    @media (min-width: 640px) {
        .app-footer {
            flex-direction: row;
            justify-content: space-between;
        }
    }
</style>