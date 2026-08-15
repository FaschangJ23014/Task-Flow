<script lang="ts">
    import { goto } from '$app/navigation';
    const API_URL = "http://localhost:5121/api";

    let Username: string = $state("");
    let Password: string = $state("");
    let isAuthenticated: boolean = $state(false);

    let loginState: boolean = $state(true);
    let registerState: boolean = $state(true);

    let isFormValid = $derived(Username.trim() == "" || Password.trim() == "" || Password.length < 8);

    async function loginUser(username: string, password: string) {
        try {
            const response = await fetch(`${API_URL}/auth/login`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) {
                loginState = false;
                return;
            }

            const data = await response.json();
            if (data.token) {
                localStorage.setItem("token", data.token);
                isAuthenticated = true;
                loginState = true;
                goto("/dashboard");
            } 
        } catch (error) {
            console.error("Error logging in:", error);
            loginState = false;
        }
    }

    async function registerUser(username: string, password: string) {
        try {
            await fetch(`${API_URL}/auth/register`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) {
            registerState = false; 
            return;
        }

        registerState = true;
        loginState = true;

        } catch (error) {
            console.error("Error registering:", error);
            registerState = false;
        }
    }
</script>

    <div class="auth-box">
        <div class="header-text">
            <h2>Login</h2>
            <p>Willkommen zurück! Bitte Daten eingeben.</p>
        </div>
        
        {#if !loginState }
            <div class="error-banner">
                <span class="error-icon">⚠️</span>
                <span>Anmeldedaten sind falsch. Bitte versuche es erneut.</span>
            </div>
        {:else if !registerState }
            <div class="error-banner">
                <span class="error-icon">⚠️</span>
                <span>Registrierung fehlgeschlagen. Konto existiert bereits.</span>
            </div>
        {/if} 
    

        <div class="input-group">
            <div class="field">
                <label>Username</label>
                <input type="text" placeholder="Username" bind:value={Username} oninput={() => {loginState = true; registerState = true;}} />
            </div>
            <div class="field">
                <label>Password</label>
                <input type="password" placeholder="Password" bind:value={Password} oninput={() => {loginState = true; registerState = true;}} />
            </div>
        </div>

        <div class="button-group">
            <button class="btn-register" onclick={() => registerUser(Username, Password)} disabled={isFormValid}>Registrieren</button>
            <button class="btn-login" onclick={() => loginUser(Username, Password)} disabled={isFormValid}>Einloggen</button>
        </div>
    </div>

<style>
    /* 1. Das macht den gesamten Bildschirm-Hintergrund außerhalb der Box zum geilen Verlauf */
    :global(html), :global(body) {
        margin: 0;
        padding: 0;
        width: 100vw;
        height: 100vh;
        overflow: hidden;
        background: linear-gradient(135deg, #020604 0%, #061a14 50%, #09090b 100%) !important;
    }

    /* 2. Zentriert die Box perfekt auf dem Bildschirm */
    :global(body) {
        display: flex;
        justify-content: center;
        align-items: center;
    }

    /* 3. Deine Login-Box bleibt exakt so wie sie ist (unverändert) */
    .auth-box {
        width: 100%;
        max-width: 28rem;
        background-color: #18181b;
        border: 1px solid #27272a;
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
    }

    .field input:focus {
        border-color: #059669;
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
    }

    .btn-register {
        background-color: #27272a;
        color: #d4d4d8;
    }
    .btn-register:hover { background-color: #3f3f46; }

    .btn-login {
        background-color: #059669;
        color: #ffffff;
    }
    .btn-login:hover { background-color: #047857; }

    button:disabled {
        opacity: 0.4;
        cursor: not-allowed;
        background-color: #27272a !important;
        color: #71717a !important;
        box-shadow: none !important;
    }

    .error-banner {
        display: flex;
        align-items: center;
        gap: 10px;
        background-color: rgba(239, 68, 68, 0.15);
        border: 1px solid #ef4444;
        color: #fca5a5;
        padding: 12px 16px;
        border-radius: 8px;
        margin-bottom: 20px;
        font-size: 14px;
        font-weight: 500;
        animation: shake 0.3s ease-in-out;
    }

    .error-icon {
        font-size: 16px;
    }

    @keyframes shake {
        0%, 100% { transform: translateX(0); }
        25% { transform: translateX(-4px); }
        75% { transform: translateX(4px); }
    }

    
</style>