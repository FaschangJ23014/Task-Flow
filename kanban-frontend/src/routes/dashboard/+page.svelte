<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';

    let isLoading = $state(true);

    onMount(() => {
        const token = localStorage.getItem("token");
        
        // Wenn kein Token da ist, sofort gnadenlos zum Login zurück!
        if (!token) {
            goto("/"); 
        } else {
            isLoading = false;
        }
    });

    function logout() {
        localStorage.removeItem("token");
        goto("/");
    }
</script>

{#if isLoading}
    <div class="loading-screen">
        Lade Dashboard...
    </div>
{:else}
    <div class="dashboard-container">
        <header class="dashboard-header">
            <h1>Dein Kanban Board</h1>
            <button class="btn-logout" onclick={logout}>Ausloggen</button>
        </header>
        
        <main class="board-content">
            <p>Willkommen im geschützten Bereich!</p>
        </main>
    </div>
{/if}

<style>
    :global(html), :global(body) {
        margin: 0;
        padding: 0;
        width: 100vw;
        height: 100vh;
        background: linear-gradient(135deg, #020604 0%, #061a14 50%, #09090b 100%) !important;
        color: #ffffff;
        font-family: inherit;
    }

    .loading-screen {
        background: #09090b;
        color: white;
        height: 100vh;
        display: flex;
        justify-content: center;
        align-items: center;
        font-size: 1rem;
    }

    .dashboard-container {
        display: flex;
        flex-direction: column;
        height: 100vh;
        padding: 2rem;
        box-sizing: border-box;
    }

    .dashboard-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        border-bottom: 1px solid rgba(5, 150, 105, 0.2);
        padding-bottom: 1rem;
    }

    .dashboard-header h1 {
        font-size: 1.5rem;
        margin: 0;
    }

    .btn-logout {
        background-color: #ef4444;
        color: white;
        border: none;
        padding: 0.5rem 1rem;
        border-radius: 0.5rem;
        cursor: pointer;
        font-weight: 500;
    }

    .btn-logout:hover {
        background-color: dc2626;
    }

    .board-content {
        padding-top: 2rem;
    }
</style>