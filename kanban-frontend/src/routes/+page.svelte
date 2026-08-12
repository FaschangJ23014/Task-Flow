<script lang="ts">
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
        min-height: 100vh;
        background-color: #09090b;
        color: #f4f4f5;
        display: flex;
        flex-direction: column;
        font-family: system-ui, -apple-system, sans-serif;
    }

    .container {
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 1rem;
    }

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