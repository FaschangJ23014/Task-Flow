<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    // Importiere jetzt auch create und update!
    import { getMyTasks, getTasksByTeam, createKanbanTask, updateKanbanTask } from '$lib/services/api';

    let isLoading: boolean = $state(true);
    let tasks: Task[] = $state([]);
    
    // Popups
    let showTeamPopup: boolean = $state(false);
    let showSettingsPopup: boolean = $state(false);
    let showCreateTaskPopup: boolean = $state(false); // NEU: Popup für neuen Task
    
    // Felder für neuen Task
    let newTaskTitle = $state("");
    let newTaskDesc = $state("");

    // Aktuelles Team (0 bedeutet privater Task)
    let currentTeamId: number = $state(0); 

    export interface Task {
        id: number;
        title: string;
        description: string;
        status: 'Todo' | 'in-progress' | 'done';
    }

    // Lädt die Tasks (wird beim Start und nach dem Erstellen aufgerufen)
    async function loadTasks() {
        try {
            if (currentTeamId > 0) {
                tasks = await getTasksByTeam(currentTeamId);
            } else {
                tasks = await getMyTasks();
            }
        } catch (error) {
            console.error("Fehler beim Laden der Tasks:", error);
        }
    }

    onMount(async () => {
        const token = localStorage.getItem("token");
        if (!token) {
            goto("/"); 
            return;
        }
        await loadTasks();
        isLoading = false;
    });

    // --- NEU: Task erstellen ---
    async function handleCreateTask() {
        if (!newTaskTitle.trim()) return; // Leere Titel verhindern
        
        try {
            // Status ist standardmäßig immer 'Todo'
            await createKanbanTask(newTaskTitle, newTaskDesc, 'Todo', currentTeamId);
            
            // Popup schließen & Felder leeren
            showCreateTaskPopup = false;
            newTaskTitle = "";
            newTaskDesc = "";
            
            // Tasks neu laden, damit der neue Task auftaucht
            await loadTasks();
        } catch (err) {
            console.error(err);
            alert("Fehler beim Erstellen des Tasks.");
        }
    }

    // --- NEU: Task verschieben (Pfeile) ---
    async function moveTask(task: Task, newStatus: 'Todo' | 'in-progress' | 'done') {
        try {
            // 1. API Aufruf an dein .NET Backend
            await updateKanbanTask(task.id, task.title, task.description, newStatus);
            
            // 2. Lokales Array sofort updaten, damit die UI direkt reagiert!
            const index = tasks.findIndex(t => t.id === task.id);
            if (index !== -1) {
                tasks[index].status = newStatus;
            }
        } catch (err) {
            console.error(err);
            alert("Fehler beim Verschieben des Tasks.");
        }
    }

    // Filter für die Spalten
    let todoTasks = $derived(tasks.filter(t => t.status === 'Todo'));
    let inProgressTasks = $derived(tasks.filter(t => t.status === 'in-progress'));
    let doneTasks = $derived(tasks.filter(t => t.status === 'done'));

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
        
        <!-- 1. LINKE SIDEBAR -->
        <aside class="sidebar-left">
            <h2>Teams</h2>
            <button class="btn-primary" onclick={() => showTeamPopup = true}>+ Team beitreten / erstellen</button>
            <div class="team-list">
                {#if currentTeamId > 0}
                    <p class="active-team">🚀 Aktives Team ID: {currentTeamId}</p>
                {:else}
                    <p class="active-team" style="color: #a1a1aa;">Privater Bereich</p>
                {/if}
            </div>
        </aside>
        
        <!-- 2. ZENTRUM: Kanban Board -->
        <main class="kanban-main">
            <header class="board-header">
                <h1>Kanban Board {currentTeamId > 0 ? '(Team-Ansicht)' : '(Deine Tasks)'}</h1>
            </header>

            <div class="kanban-columns">
                
                <!-- Spalte 1: Todo -->
                <div class="column">
                    <h3>Todo <span class="task-count">({todoTasks.length})</span></h3>
                    {#each todoTasks as task (task.id)}
                        <div class="task-card">
                            <div class="task-content">
                                <h4>{task.title}</h4>
                                <p>{task.description}</p>
                            </div>
                            <!-- Pfeil nach rechts -->
                            <div class="task-actions">
                                <div></div> <!-- Platzhalter links -->
                                <button class="btn-arrow" onclick={() => moveTask(task, 'in-progress')} title="Verschieben nach In Progress">➔</button>
                            </div>
                        </div>
                    {:else}
                        <p class="empty-text">Alles erledigt!</p>
                    {/each}
                </div>

                <!-- Spalte 2: In Progress -->
                <div class="column">
                    <h3>In Progress <span class="task-count">({inProgressTasks.length})</span></h3>
                    {#each inProgressTasks as task (task.id)}
                        <div class="task-card">
                            <div class="task-content">
                                <h4>{task.title}</h4>
                                <p>{task.description}</p>
                            </div>
                            <!-- Pfeile in beide Richtungen -->
                            <div class="task-actions">
                                <button class="btn-arrow" onclick={() => moveTask(task, 'Todo')} title="Zurück zu Todo">⬅</button>
                                <button class="btn-arrow" onclick={() => moveTask(task, 'done')} title="Abschließen">➔</button>
                            </div>
                        </div>
                    {:else}
                        <p class="empty-text">Nichts in Arbeit</p>
                    {/each}
                </div>

                <!-- Spalte 3: Done -->
                <div class="column">
                    <h3>Done <span class="task-count">({doneTasks.length})</span></h3>
                    {#each doneTasks as task (task.id)}
                        <div class="task-card">
                            <div class="task-content">
                                <h4>{task.title}</h4>
                                <p>{task.description}</p>
                            </div>
                            <!-- Pfeil nach links -->
                            <div class="task-actions">
                                <button class="btn-arrow" onclick={() => moveTask(task, 'in-progress')} title="Zurück in Bearbeitung">⬅</button>
                                <div></div> <!-- Platzhalter rechts -->
                            </div>
                        </div>
                    {:else}
                        <p class="empty-text">Noch nichts erledigt</p>
                    {/each}
                </div>
            </div>
        </main>

        <!-- 3. RECHTE SIDEBAR -->
        <aside class="sidebar-right">
            <div class="user-profile-section">
                <button class="btn-settings" onclick={() => showSettingsPopup = true}>⚙️ Settings</button>
            </div>

            <div class="stats-card">
                <h4>Statistik</h4>
                <p>Erledigte Tasks: {doneTasks.length} / {tasks.length}</p>
            </div>

            <!-- NEU: Task Erstellen Button direkt unter der Statistik -->
            <div class="action-section">
                <button class="btn-primary" style="width: 100%;" onclick={() => showCreateTaskPopup = true}>
                    + Neuen Task erstellen
                </button>
            </div>

            <div class="team-members">
                <h4>Team Mitglieder</h4>
                <ul>
                    <li>👤 Jakob (Admin)</li>
                </ul>
            </div>
        </aside>
    </div>

    <!-- POPUP: Neuen Task erstellen -->
    {#if showCreateTaskPopup}
        <div class="modal-backdrop" onclick={() => showCreateTaskPopup = false}>
            <div class="modal-content" onclick={(e) => e.stopPropagation()}>
                <h3>Neuen Task erstellen</h3>
                
                <div class="form-group">
                    <label for="title">Titel</label>
                    <input id="title" type="text" bind:value={newTaskTitle} placeholder="z.B. API anbinden" />
                </div>

                <div class="form-group">
                    <label for="desc">Beschreibung</label>
                    <textarea id="desc" bind:value={newTaskDesc} placeholder="Kurze Beschreibung..." rows="3"></textarea>
                </div>

                <div class="modal-actions">
                    <button class="btn-close" onclick={() => showCreateTaskPopup = false}>Abbrechen</button>
                    <button class="btn-primary" onclick={handleCreateTask}>Erstellen</button>
                </div>
            </div>
        </div>
    {/if}

    <!-- POPUP: Team erstellen / beitreten (Platzhalter) -->
    {#if showTeamPopup}
        <!-- (Code für Team-Popup bleibt gleich...) -->
        <div class="modal-backdrop" onclick={() => showTeamPopup = false}>
            <div class="modal-content" onclick={(e) => e.stopPropagation()}>
                <h3>Team verwalten</h3>
                <button class="btn-close" onclick={() => showTeamPopup = false}>Schließen</button>
            </div>
        </div>
    {/if}

    <!-- POPUP: Settings & Logout -->
    {#if showSettingsPopup}
        <div class="modal-backdrop" onclick={() => showSettingsPopup = false}>
            <div class="modal-content" onclick={(e) => e.stopPropagation()}>
                <h3>Einstellungen</h3>
                <button class="btn-logout" onclick={logout}>Ausloggen</button>
                <button class="btn-close" onclick={() => showSettingsPopup = false}>Schließen</button>
            </div>
        </div>
    {/if}
{/if}

<style>
    /* ... (Die bestehenden globalen & Layout Styles bleiben gleich) ... */
    :global(html), :global(body) { margin: 0; padding: 0; width: 100vw; height: 100vh; background: linear-gradient(135deg, #020604 0%, #061a14 50%, #09090b 100%) !important; color: #ffffff; font-family: inherit; overflow-x: hidden; }
    .loading-screen { background: #09090b; color: white; height: 100vh; display: flex; justify-content: center; align-items: center; }
    .dashboard-layout { display: grid; grid-template-columns: 260px 1fr 280px; height: 100vh; box-sizing: border-box; }
    .sidebar-left, .sidebar-right { background-color: rgba(24, 24, 27, 0.6); border: 1px solid rgba(39, 39, 42, 0.5); padding: 1.5rem; display: flex; flex-direction: column; gap: 1.5rem; box-sizing: border-box; overflow-y: auto; }
    .sidebar-left { border-left: none; border-top: none; border-bottom: none; }
    .sidebar-right { border-right: none; border-top: none; border-bottom: none; }
    .kanban-main { padding: 2rem; display: flex; flex-direction: column; gap: 1.5rem; overflow-y: auto; }
    .board-header h1 { margin: 0; font-size: 1.5rem; }
    .kanban-columns { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1.5rem; flex: 1; }
    
    .column { background-color: rgba(24, 24, 27, 0.4); border: 1px solid #27272a; border-radius: 0.75rem; padding: 1rem; display: flex; flex-direction: column; gap: 1rem; min-width: 200px; }
    .column h3 { margin: 0; font-size: 1rem; color: #a1a1aa; border-bottom: 1px solid #27272a; padding-bottom: 0.5rem; display: flex; justify-content: space-between; }
    .task-count { font-size: 0.8rem; background: #27272a; padding: 0.1rem 0.5rem; border-radius: 1rem; color: #fff; }

    /* NEU: Das Task-Card Design inkl. Pfeile */
    .task-card {
        background-color: #18181b;
        border: 1px solid #27272a;
        padding: 1rem;
        border-radius: 0.5rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
        transition: border-color 0.2s;
    }
    .task-card:hover { border-color: #059669; }
    
    .task-content h4 { margin: 0 0 0.5rem 0; color: #fff; }
    .task-content p { margin: 0; color: #a1a1aa; font-size: 0.85rem; line-height: 1.4; }

    .task-actions {
        display: flex;
        justify-content: space-between;
        align-items: center;
        border-top: 1px solid #27272a;
        padding-top: 0.5rem;
        margin-top: auto;
    }

    .btn-arrow {
        background: transparent;
        border: none;
        color: #52525b;
        font-size: 1.2rem;
        cursor: pointer;
        padding: 0 0.5rem;
        transition: color 0.2s;
    }
    .btn-arrow:hover { color: #10b981; } /* Leuchtend Grün beim Hover */

    .empty-text { color: #52525b; font-size: 0.85rem; font-style: italic; text-align: center; margin-top: 1rem; }

    /* Buttons & Popups */
    .btn-primary { background-color: #059669; color: white; border: none; padding: 0.75rem; border-radius: 0.5rem; cursor: pointer; font-weight: 500; }
    .btn-primary:hover { background-color: #047857; }
    .btn-settings { background-color: #27272a; color: white; border: none; width: 100%; padding: 0.75rem; border-radius: 0.5rem; cursor: pointer; }
    .btn-settings:hover { background-color: #3f3f46; }
    .btn-logout { background-color: #ef4444; color: white; border: none; padding: 0.75rem; border-radius: 0.5rem; cursor: pointer; }
    .btn-logout:hover { background-color: #dc2626; }
    .btn-close { background-color: #27272a; color: white; border: none; padding: 0.5rem 1rem; border-radius: 0.5rem; cursor: pointer; }

    .modal-backdrop { position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; background: rgba(0, 0, 0, 0.7); display: flex; justify-content: center; align-items: center; z-index: 1000; }
    .modal-content { background: #18181b; border: 1px solid #27272a; padding: 2rem; border-radius: 1rem; width: 90%; max-width: 400px; display: flex; flex-direction: column; gap: 1rem; }
    
    /* Formulare im Modal */
    .form-group { display: flex; flex-direction: column; gap: 0.5rem; }
    .form-group label { font-size: 0.85rem; color: #a1a1aa; }
    .form-group input, .form-group textarea {
        background: #09090b; border: 1px solid #27272a; padding: 0.75rem; border-radius: 0.5rem; color: white; font-family: inherit;
    }
    .form-group input:focus, .form-group textarea:focus { border-color: #059669; outline: none; }
    .modal-actions { display: flex; justify-content: flex-end; gap: 1rem; margin-top: 1rem; }

    @media (max-width: 1024px) { .dashboard-layout { grid-template-columns: 200px 1fr 220px; } }
    @media (max-width: 768px) {
        :global(html), :global(body) { height: auto; overflow-y: auto; }
        .dashboard-layout { display: flex; flex-direction: column; height: auto; }
        .sidebar-left, .sidebar-right { width: 100%; border: none; border-bottom: 1px solid rgba(39, 39, 42, 0.5); }
        .kanban-main { padding: 1rem; }
        .kanban-columns { grid-template-columns: 1fr; }
    }
</style>