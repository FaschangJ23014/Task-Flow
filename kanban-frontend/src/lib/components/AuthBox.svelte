<script lang="ts">
    import { goto } from '$app/navigation';
    const API_URL = "http://localhost:5121/api";

    let Username: string = $state("");
    let Password: string = $state("");
    let isAuthenticated: boolean = $state(false);

    let loginState: boolean = $state(true);
    let registerState: boolean = $state(true);
    let registerSuccess: boolean = $state(false);

    let isFormValid = $derived(Username.trim() == "" || Password.trim() == "" || Password.length < 8 || Username.length < 3 || Username.length > 20);

    async function loginUser(username: string, password: string) {
        try {
            const response = await fetch(`${API_URL}/auth/login`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) {
                loginState = false;
                registerSuccess = false;
                return;
            }

            const data = await response.json();
            if (data.token) {
                localStorage.setItem("token", data.token);
                localStorage.setItem("username", Username);

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
            const res = await fetch(`${API_URL}/auth/register`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password })
            });

            if (!res.ok) {
                registerState = false; 
                registerSuccess = false;
                return;
            }

            registerState = true;
            loginState = true;
            registerSuccess = true;

        } catch (error) {
            console.error("Error registering:", error);
            registerState = false;
            registerSuccess = false;
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
                <span class="banner-icon">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
                </span>
                <span>Anmeldedaten sind falsch. Bitte versuche es erneut.</span>
            </div>
        {:else if !registerState }
            <div class="error-banner">
                <span class="banner-icon">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
                </span>
                <span>Registrierung fehlgeschlagen. Konto existiert bereits.</span>
            </div>
        {:else if registerSuccess}
            <div class="success-banner">
                <span class="banner-icon">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
                </span>
                <span>Registrierung erfolgreich! Du kannst dich jetzt einloggen.</span>
            </div>
        {/if} 
    

        <div class="input-group">
            <div class="field">
                <label for="username-input">Username</label>
                <input id="username-input" type="text" placeholder="Username" bind:value={Username} oninput={() => {loginState = true; registerState = true; registerSuccess = false;}} />
            </div>
            <div class="field">
                <label for="password-input">Password</label>
                <input id="password-input" type="password" placeholder="Password" bind:value={Password} oninput={() => {loginState = true; registerState = true; registerSuccess = false;}} />
            </div>

            <!-- NEU: Schicker Info-Kasten für die Regeln -->
            <div class="info-box">
                <div class="info-item" class:valid={Username.length >= 3 && Username.length <= 20}>
                    <span class="dot"></span> Username: 3–20 Zeichen
                </div>
                <div class="info-item" class:valid={Password.length >= 8}>
                    <span class="dot"></span> Passwort: min. 8 Zeichen
                </div>
            </div>
        </div>

        <div class="button-group">
            <button class="btn-register" onclick={() => registerUser(Username, Password)} disabled={isFormValid}>Registrieren</button>
            <button class="btn-login" onclick={() => loginUser(Username, Password)} disabled={isFormValid}>Einloggen</button>
        </div>
    </div>

<style>
    :global(html), :global(body) {
        margin: 0;
        padding: 0;
        width: 100vw;
        height: 100vh;
        overflow: hidden;
        background: linear-gradient(135deg, #020604 0%, #061a14 50%, #09090b 100%) !important;
    }

    :global(body) {
        display: flex;
        justify-content: center;
        align-items: center;
    }

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

    /* NEU: Styling für die Info-Box */
    .info-box {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
        background-color: rgba(9, 9, 11, 0.6);
        border: 1px solid #27272a;
        padding: 0.75rem 1rem;
        border-radius: 0.75rem;
        font-size: 0.75rem;
        color: #71717a;
    }

    .info-item {
        display: flex;
        align-items: center;
        gap: 6px;
        transition: color 0.2s ease;
    }

    .dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background-color: #52525b;
        transition: background-color 0.2s ease;
    }

    /* Wechselt die Farbe zu Grün, wenn die Bedingung erfüllt ist! */
    .info-item.valid {
        color: #34d399;
    }

    .info-item.valid .dot {
        background-color: #059669;
        box-shadow: 0 0 6px rgba(5, 150, 105, 0.6);
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

    .success-banner {
        display: flex;
        align-items: center;
        gap: 10px;
        background-color: rgba(5, 150, 105, 0.15);
        border: 1px solid #059669;
        color: #34d399;
        padding: 12px 16px;
        border-radius: 8px;
        margin-bottom: 20px;
        font-size: 14px;
        font-weight: 500;
        animation: fadeIn 0.3s ease-in-out;
    }

    .banner-icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
    }

    @keyframes shake {
        0%, 100% { transform: translateX(0); }
        25% { transform: translateX(-4px); }
        75% { transform: translateX(4px); }
    }

    @keyframes fadeIn {
        from { opacity: 0; transform: translateY(-4px); }
        to { opacity: 1; transform: translateY(0); }
    }
</style>