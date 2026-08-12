<script lang="ts">
    import { goto } from '$app/navigation';
    const API_URL = "http://localhost:5121/api";

    let Username: string = $state("");
    let Password: string = $state("");
    let isAuthenticated = $state(false);

    let isFormValid = $derived(Username.trim() == "" || Password.trim() == "");

    async function loginUser(username: string, password: string) {
        try {
            const response = await fetch(`${API_URL}/auth/login`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) throw new Error("Login failed");

            const data = await response.json();
            if (data.token) {
                localStorage.setItem("token", data.token);
                isAuthenticated = true;
                goto("/dashboard");
            } 
        } catch (error) {
            console.error("Error logging in:", error);
        }
    }

    async function registerUser(username: string, password: string) {
        try {
            const response = await fetch(`${API_URL}/auth/register`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) throw new Error("Registration failed");
            
            alert("Erfolgreich registriert! Du kannst dich jetzt einloggen.");
        } catch (error) {
            console.error("Error registering:", error);
        }
    }
</script>

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
        <button class="btn-register" onclick={() => registerUser(Username, Password)} disabled={isFormValid}>Registrieren</button>
        <button class="btn-login" onclick={() => loginUser(Username, Password)} disabled={isFormValid}>Einloggen</button>
    </div>
</div>

<style>
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
</style>