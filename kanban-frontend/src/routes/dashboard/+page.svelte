<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import { getMyTasks, getTasksByTeam, createKanbanTask, updateKanbanTask, deleteKanbanTask, registerTeam, joinTeam, getTeamMembers} from '$lib/services/api';
    import * as signalR from "@microsoft/signalr";

    let isLoading: boolean = $state(true);
    let tasks: Task[] = $state([]);
    
    // Popups
    let showTeamPopup: boolean = $state(false);
    let showSettingsPopup: boolean = $state(false);
    let showCreateTaskPopup: boolean = $state(false); 
    
    // Felder für neuen Task
    let newTaskTitle = $state("");
    let newTaskDesc = $state("");

    // Aktuelles Team (0 bedeutet privater Task)
    let currentTeamId: number = $state(0); 

    //Felder für Team erstellen/beitreten
    let teamName = $state("");
    let teamPassword = $state("");

    let teamMembers = $state<string[]>([]);

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


    // JWT-Token auslesen, um die TeamId zu ermitteln
    function getTeamIdFromToken(token: string): number {
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            const payload = JSON.parse(jsonPayload);
            return payload.TeamId ? parseInt(payload.TeamId) : 0;
        } catch (e) {
            return 0;
        }
    }


    // Diese Funktion rufst du auf, wenn du Tasks lädst oder ein Team beitrittst
    async function loadTeamMembersList() {
        if (currentTeamId > 0) {
            try {
                const members = await getTeamMembers(currentTeamId);
                // Mappt die API-Antwort [{id, username}, ...] zu einem reinen String-Array der Usernamen
                teamMembers = members.map((m: any) => m.username);
            } catch (err) {
                console.error("Fehler beim Laden der Team-Mitglieder:", err);
                teamMembers = [];
            }
        } else {
            teamMembers = [];
        }
    }

   onMount(async () => {
    const token = localStorage.getItem("token");
    if (!token) {
        goto("/"); 
        return;
    }

    currentTeamId = getTeamIdFromToken(token);
    await loadTasks();
    await loadTeamMembersList();
    isLoading = false;

    // --- SIGNALR LIVE VERBINDUNG ---
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5121/kanbanHub", { 
            accessTokenFactory: () => token,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .build();

    // Hört auf das Signal vom Backend (z.B. "ReceiveTaskUpdate" oder "TaskUpdated")
    connection.on("ReceiveTaskUpdate", async (message) => {
        console.log("Live-Update empfangen:", message);
        await loadTasks(); // Lädt die Tasks automatisch neu, wenn jemand was ändert!
    });

    connection.on("UserJoined", async (message) => {
    console.log("Neues Mitglied:", message);
    await loadTeamMembersList(); // Lädt die Liste der Mitglieder neu
});

    try {
        await connection.start();
        console.log("SignalR verbunden!");
    } catch (err) {
        console.error("SignalR Verbindungsfehler: ", err);
    }
});

    async function handleCreateTask() {
        if (!newTaskTitle.trim()) return; 
        
        try {
            await createKanbanTask(newTaskTitle, newTaskDesc, 'Todo', currentTeamId);
            showCreateTaskPopup = false;
            newTaskTitle = "";
            newTaskDesc = "";
            await loadTasks();
        } catch (err) {
            console.error(err);
            alert("Fehler beim Erstellen des Tasks.");
        }
    }

    //Team erstellen oder beitreten
    async function handleTeamAction(action: 'create' | 'join') {
        if (!teamName.trim() || !teamPassword.trim()) return;

        try {
            if (action === 'create') {
                await registerTeam(teamName, teamPassword);
                alert("Team erfolgreich erstellt!");
            } else if (action === 'join') {
                const newToken = await joinTeam(teamName, teamPassword); // Hier kommt das Token zurück
                if (newToken) {
                    localStorage.setItem("token", newToken); // <--- WICHTIG: Neues Token speichern!
                    currentTeamId = getTeamIdFromToken(newToken); // TeamId aktualisieren
                }
                alert("Team erfolgreich beigetreten!");
                await loadTasks();
                await loadTeamMembersList();
                window.location.reload(); 
            }
            showTeamPopup = false;
            teamName = "";
            teamPassword = "";
        } catch (err) {
            console.error(err);
            alert(`Fehler beim ${action === 'create' ? 'Erstellen' : 'Beitreten'} des Teams.`);
        }
    }

    async function moveTask(task: Task, newStatus: 'Todo' | 'in-progress' | 'done') {
        try {
            await updateKanbanTask(task.id, task.title, task.description, newStatus);
            const index = tasks.findIndex(t => t.id === task.id);
            if (index !== -1) {
                tasks[index].status = newStatus;
            }
        } catch (err) {
            console.error(err);
            alert("Fehler beim Verschieben des Tasks.");
        }
    }

    // HIER ANGEPASST: Einheitlicher Name für den Aufruf im HTML
    async function handleDelete(taskId: number) {
        if (!confirm("Mist du diesen Task wirklich löschen?")) return;

        try {
            await deleteKanbanTask(taskId);
            tasks = tasks.filter(t => t.id !== taskId);
        } catch (error) {
            console.error(error);
            alert("Fehler beim Löschen des Tasks");
        }
    }

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
                            <div class="task-actions">
                                <button class="btn-delete" onclick={() => handleDelete(task.id)} title="Task löschen">🗑️</button>
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
                            <div class="task-actions">
                                <div style="display: flex; gap: 0.5rem; align-items: center;">
                                    <button class="btn-delete" onclick={() => handleDelete(task.id)} title="Task löschen">🗑️</button>
                                    <button class="btn-arrow" onclick={() => moveTask(task, 'Todo')} title="Zurück zu Todo">⬅</button>
                                </div>
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
                            <div class="task-actions">
                                <button class="btn-delete" onclick={() => handleDelete(task.id)} title="Task löschen">🗑️</button>
                                <button class="btn-arrow" onclick={() => moveTask(task, 'in-progress')} title="Zurück in Bearbeitung">⬅</button>
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

            <div class="action-section">
                <button class="btn-primary" style="width: 100%;" onclick={() => showCreateTaskPopup = true}>
                    + Neuen Task erstellen
                </button>
            </div>

            <div class="team-members">
                <h4>Team Mitglieder</h4>
                <ul>
                    {#each teamMembers as member}
                        <li>👤 {member}</li>
                    {:else}
                        <li style="color: #52525b; font-size: 0.8rem;">Keine weiteren Mitglieder</li>
                    {/each}
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

    <!-- POPUP: Team verwalten -->
    <!-- POPUP: Team erstellen / beitreten -->
    {#if showTeamPopup}
        <div class="modal-backdrop" onclick={() => showTeamPopup = false}>
            <div class="modal-content" onclick={(e) => e.stopPropagation()}>
                <h3>Team verwalten</h3>
                
                <div class="form-group">
                    <label for="teamName">Team Name</label>
                    <input id="teamName" type="text" bind:value={teamName} placeholder="z.B. Entwickler-Team" />
                </div>

                <div class="form-group">
                    <label for="teamPass">Passwort</label>
                    <input id="teamPass" type="password" bind:value={teamPassword} placeholder="Geheimes Passwort..." />
                </div>

                <div class="modal-actions" style="flex-direction: column; gap: 0.5rem;">
                    <button class="btn-primary" onclick={() => handleTeamAction('create')}>
                        Team erstellen
                    </button>
                    <button class="btn-secondary" onclick={() => handleTeamAction('join')} style="background: #3f3f46;">
                        Team beitreten
                    </button>
                    <button class="btn-close" onclick={() => showTeamPopup = false} style="margin-top: 1rem;">
                        Abbrechen
                    </button>
                </div>
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
    .btn-arrow:hover { color: #10b981; }

    .btn-delete {
        background: none;
        border: none;
        cursor: pointer;
        font-size: 1rem;
        opacity: 0.7;
        transition: opacity 0.2s;
    }
    .btn-delete:hover {
        opacity: 1;
    }

    .empty-text { color: #52525b; font-size: 0.85rem; font-style: italic; text-align: center; margin-top: 1rem; }

    .btn-primary { background-color: #059669; color: white; border: none; padding: 0.75rem; border-radius: 0.5rem; cursor: pointer; font-weight: 500; }
    .btn-primary:hover { background-color: #047857; }
    .btn-settings { background-color: #27272a; color: white; border: none; width: 100%; padding: 0.75rem; border-radius: 0.5rem; cursor: pointer; }
    .btn-settings:hover { background-color: #3f3f46; }
    .btn-logout { background-color: #ef4444; color: white; border: none; padding: 0.75rem; border-radius: 0.5rem; cursor: pointer; }
    .btn-logout:hover { background-color: #dc2626; }
    .btn-close { background-color: #27272a; color: white; border: none; padding: 0.5rem 1rem; border-radius: 0.5rem; cursor: pointer; }

    .modal-backdrop { position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; background: rgba(0, 0, 0, 0.7); display: flex; justify-content: center; align-items: center; z-index: 1000; }
    .modal-content { background: #18181b; border: 1px solid #27272a; padding: 2rem; border-radius: 1rem; width: 90%; max-width: 400px; display: flex; flex-direction: column; gap: 1rem; }
    
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

    .task-card {
    background-color: #18181b;
    border: 1px solid #27272a;
    padding: 1rem;
    border-radius: 0.5rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
    transition: border-color 0.2s;
    
    /* WICHTIG: Verhindert, dass Elemente gesprengt werden */
    min-width: 0; 
    word-break: break-word; 
}

.task-content {
    /* WICHTIG: Erlaubt dem Inhalt, sich anzupassen */
    min-width: 0; 
}

.task-content h4 {
    margin: 0 0 0.5rem 0;
    color: #fff;
    
    /* Verhindert horizontales Überlaufen bei langen Wörtern */
    overflow-wrap: break-word;
    word-wrap: break-word;
}

.task-content p {
    margin: 0;
    color: #a1a1aa;
    font-size: 0.85rem;
    line-height: 1.4;
    
    /* Verhindert Überlaufen bei langen Texten */
    overflow-wrap: break-word;
    word-wrap: break-word;
    
    /* OPTIONAL: Wenn du den Text auf z.B. maximal 3 Zeilen begrenzen willst 
       und den Rest abschneiden möchtest (mit ... am Ende): */
    /*
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
    */
}


/* Basis-Button-Design (falls noch nicht vorhanden) */
button {
    padding: 0.6rem 1rem;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-weight: 500;
    transition: background 0.2s;
}

/* Dein vorhandener btn-primary (wahrscheinlich grün oder blau) */
.btn-primary {
    background: #0d9d0d; /* Blau-Ton */
    color: white;
}

.btn-primary:hover {
    background: #1d4ed8;
}

/* NEU: Das Design für btn-secondary */
.btn-secondary {
    background: #64748b; /* Ein neutrales Schiefergrau */
    color: white;
}

.btn-secondary:hover {
    background: #475569;
}

/* Design für den Schließen-Button */
.btn-close {
    background: transparent;
    color: #ef4444; /* Rot für Abbrechen */
    border: 1px solid #ef4444;
}

.btn-close:hover {
    background: #fee2e2;
}
</style>