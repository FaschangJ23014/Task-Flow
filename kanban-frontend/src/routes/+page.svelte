<script lang="ts">
import { onMount } from 'svelte';
import { goto } from '$app/navigation';

import AuthBox from "$lib/components/AuthBox.svelte";

</script>

<div class="page-wrapper">
    <main class="container">
        <AuthBox />



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
    .page-wrapper {
        width: 100vw;
        min-height: 100vh;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        justify-content: space-between; /* Verteilt Login oben/Mitte und Footer nach unten */
        align-items: center;
        background: linear-gradient(135deg, #020604 0%, #061a14 50%, #09090b 100%);
        box-sizing: border-box;
    }

    .container {
        width: 100% !important;
        max-width: 100% !important;
        margin: auto !important; /* Zentriert die Login-Box perfekt vertikal und horizontal */
        padding: 2rem;
        display: flex;
        justify-content: center;
        align-items: center;
        box-sizing: border-box;
    }

    .app-footer {
        width: 100%;
        max-width: 1200px;
        padding: 1.5rem 2rem;
        /* Hartes Schwarz entfernt und durch einen edlen, transparenten Smaragd-Look ersetzt */
        border-top: 1px solid rgba(5, 150, 105, 0.2);
        background: transparent;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.75rem;
        font-size: 0.8rem;
        color: #71717a;
        text-align: center;
        box-sizing: border-box;
    }

    .footer-links {
        display: flex;
        gap: 1.5rem;
    }

    .footer-links a {
        color: #a1a1aa;
        text-decoration: none;
        transition: color 0.2s ease;
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