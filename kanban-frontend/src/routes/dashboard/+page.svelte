<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';

    let isLoading: boolean = $state(true);
    let Tasks: Task[] = $state([]);
    let showTeamPopup: boolean = $state(false);
    let showSettingsPopup: boolean = $state(false);

    onMount(() => {
        const token = localStorage.getItem("token");
        
        // Wenn kein Token da ist, sofort gnadenlos zum Login zurück!
        if (!token) {
            goto("/"); 
        } else {
            isLoading = false;
        }
    });

    export interface Task {
        id: number;
        title: string;
        description: string;
        status: 'Todo' | 'in-progress' | 'done';
    }

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
    <div class="dashboard-layout">
        
        <!-- 1. LINKE SIDEBAR: Teams -->
        <aside class="sidebar-left">
            <h2>Teams</h2>
            <button class="btn-primary" onclick={() => showTeamPopup = true}>+ Team beitreten / erstellen</button>
            <div class="team-list">
                <p class="active-team">🚀 TaskFlow Core Team</p>
            </div>
        </aside>
        
        <main class="kanban-main">
            <header class="board-header">
                <h1>Kanban Board</h1>
            </header>

            <div class="kanban-columns">
                <!-- Spalte 1: Todo -->
                <div class="column">
                    <h3>Todo</h3>
                    <div class="task-card">Beispiel Task 1</div>
                </div>
                <!-- Spalte 2: In Progress -->
                <div class="column">
                    <h3>In Progress</h3>
                    <div class="task-card">Beispiel Task 2</div>
                </div>
                <!-- Spalte 3: Done -->
                <div class="column">
                    <h3>Done</h3>
                    <div class="task-card">Beispiel Task 3</div>
                </div>
            </div>
        </main>

        <!-- 3. RECHTE SIDEBAR: Stats, Mitglieder & Settings -->
        <aside class="sidebar-right">
            <div class="user-profile-section">
                <button class="btn-settings" onclick={() => showSettingsPopup = true}>⚙️ Settings</button>
            </div>

            <div class="stats-card">
                <h4>Statistik</h4>
                <p>Erledigte Tasks: 3 / 5</p>
            </div>

            <div class="team-members">
                <h4>Team Mitglieder</h4>
                <ul>
                    <li>👤 Jakob Faschang (Admin)</li>
                    <li>👤 Kollege 2</li>
                </ul>
            </div>
        </aside>
    </div>

    <!-- POPUP: Team erstellen / beitreten -->
    {#if showTeamPopup}
        <div class="modal-backdrop" onclick={() => showTeamPopup = false}>
            <div class="modal-content" onclick={(e) => e.stopPropagation()}>
                <h3>Team verwalten</h3>
                <p>Hier kannst du ein Team erstellen oder beitreten.</p>
                <button class="btn-close" onclick={() => showTeamPopup = false}>Schließen</button>
            </div>
        </div>
    {/if}

    <!-- POPUP: Settings & Logout -->
    {#if showSettingsPopup}
        <div class="modal-backdrop" onclick={() => showSettingsPopup = false}>
            <div class="modal-content" onclick={(e) => e.stopPropagation()}>
                <h3>Einstellungen</h3>
                <p>Account-Verwaltung</p>
                <button class="btn-logout" onclick={logout}>Ausloggen</button>
                <button class="btn-close" onclick={() => showSettingsPopup = false}>Schließen</button>
            </div>
        </div>
    {/if}
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
        overflow-x: hidden; /* Verhindert horizontales Scrollen */
    }

    .loading-screen {
        background: #09090b;
        color: white;
        height: 100vh;
        display: flex;
        justify-content: center;
        align-items: center;
    }

    /* Standard-Layout (Desktop ab 1024px) */
    .dashboard-layout {
        display: grid;
        grid-template-columns: 260px 1fr 280px;
        height: 100vh;
        box-sizing: border-box;
    }

    /* Sidebars & Main Styling */
    .sidebar-left, .sidebar-right {
        background-color: rgba(24, 24, 27, 0.6);
        border: 1px solid rgba(39, 39, 42, 0.5);
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
        box-sizing: border-box;
        overflow-y: auto;
    }

    .sidebar-left { border-left: none; border-top: none; border-bottom: none; }
    .sidebar-right { border-right: none; border-top: none; border-bottom: none; }

    .kanban-main {
        padding: 2rem;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
        overflow-y: auto;
    }

    .board-header h1 {
        margin: 0;
        font-size: 1.5rem;
    }

    /* Kanban Spalten */
    .kanban-columns {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 1.5rem;
        flex: 1;
    }

    .column {
        background-color: rgba(24, 24, 27, 0.4);
        border: 1px solid #27272a;
        border-radius: 0.75rem;
        padding: 1rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
        min-width: 200px;
    }

    .column h3 {
        margin: 0;
        font-size: 1rem;
        color: #a1a1aa;
        border-bottom: 1px solid #27272a;
        padding-bottom: 0.5rem;
    }

    .task-card {
        background-color: #18181b;
        border: 1px solid #27272a;
        padding: 1rem;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        cursor: grab;
    }

    .task-card:hover {
        border-color: #059669;
    }

    /* Buttons & Popups */
    .btn-primary {
        background-color: #059669;
        color: white;
        border: none;
        padding: 0.75rem;
        border-radius: 0.5rem;
        cursor: pointer;
        font-weight: 500;
    }
    .btn-primary:hover { background-color: #047857; }

    .btn-settings {
        background-color: #27272a;
        color: white;
        border: none;
        width: 100%;
        padding: 0.75rem;
        border-radius: 0.5rem;
        cursor: pointer;
    }
    .btn-settings:hover { background-color: #3f3f46; }

    .modal-backdrop {
        position: fixed;
        top: 0; left: 0; width: 100vw; height: 100vh;
        background: rgba(0, 0, 0, 0.7);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 1000;
    }

    .modal-content {
        background: #18181b;
        border: 1px solid #27272a;
        padding: 2rem;
        border-radius: 1rem;
        width: 90%;
        max-width: 400px;
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    .btn-logout {
        background-color: #ef4444;
        color: white;
        border: none;
        padding: 0.75rem;
        border-radius: 0.5rem;
        cursor: pointer;
    }
    .btn-logout:hover { background-color: #dc2626; }

    .btn-close {
        background-color: #27272a;
        color: white;
        border: none;
        padding: 0.5rem;
        border-radius: 0.5rem;
        cursor: pointer;
    }

    /* --- RESPONSIVE ANPASSUNGEN (Für Handys, Tablets & kleine Fenster) --- */

    @media (max-width: 1024px) {
        /* Wenn das Fenster kleiner wird, blenden wir die Sidebars ein oder machen sie kompakter */
        .dashboard-layout {
            grid-template-columns: 200px 1fr 220px;
        }
    }

    @media (max-width: 768px) {
        /* Auf Tablets/Handys schalten wir das Grid auf eine einzige Spalte um! */
        :global(html), :global(body) {
            height: auto;
            overflow-y: auto;
        }

        .dashboard-layout {
            display: flex;
            flex-direction: column;
            height: auto;
        }

        .sidebar-left, .sidebar-right {
            width: 100%;
            border: none;
            border-bottom: 1px solid rgba(39, 39, 42, 0.5);
        }

        .kanban-main {
            padding: 1rem;
        }

        .kanban-columns {
            grid-template-columns: 1fr; /* Kanban-Spalten untereinander auf dem Handy */
        }
    }
</style>