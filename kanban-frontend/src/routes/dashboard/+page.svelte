<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import { getMyTasks, getTasksByTeam, createKanbanTask, updateKanbanTask, deleteKanbanTask, registerTeam, joinTeam, getTeamMembers, leaveTeam, changePassword, changeUsername } from '$lib/services/api';
    import * as signalR from "@microsoft/signalr";

    let isLoading: boolean = $state(true);
    let tasks: Task[] = $state([]);
    
    // Popups
    let showTeamPopup: boolean = $state(false);
    let showSettingsPopup: boolean = $state(false);
    let showCreateTaskPopup: boolean = $state(false); 
    
    // Felder für neuen Task
    let newTaskTitle: string = $state("");
    let newTaskDesc: string = $state("");

    let newPassword: string = $state("");
    let oldPassword: string = $state("");
    let newUsername: string = $state("");

    // Aktuelles Team (0 bedeutet privater Task)
    let currentTeamId: number = $state(0); 

    // Felder für Team erstellen/beitreten
    let teamName: string = $state("");
    let teamPassword: string = $state("");

    let teamMembers: string[] = $state([]);

     interface Task {
        id: number;
        title: string;
        description: string;
        status: 'Todo' | 'in-progress' | 'done';
    }


    async function handleChangeUsername() {
    if (!newUsername.trim()) {
        alert("Bitte gib einen neuen Benutzernamen ein.");
        return;
    }
    try {
        const message = await changeUsername(newUsername);
        alert(message); // Zeigt die Erfolgsmeldung vom Backend
        newUsername = "";
        showSettingsPopup = false;
    } catch (err) {
        console.error(err);
        alert("Fehler beim Ändern des Benutzernamens.");
    }
}

async function handleChangePassword() {
    if (!oldPassword || !newPassword) {
        alert("Bitte fülle alle Passwort-Felder aus.");
        return;
    }
    try {
        const message = await changePassword(oldPassword, newPassword);
        alert(message); // Zeigt die Erfolgsmeldung vom Backend
        oldPassword = "";
        newPassword = "";
        showSettingsPopup = false;
    } catch (err) {
        console.error(err);
        alert("Fehler beim Ändern des Passworts (Altes Passwort korrekt?).");
    }
}

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

    async function handleLeaveTeam() {
        if (!confirm("Willst du dieses Team wirklich verlassen?")) return;

        try {
            const success = await leaveTeam();
            if (success) {
                alert("Du hast das Team verlassen. Bitte logge dich kurz neu ein, um deinen Workspace zu aktualisieren.");
                localStorage.removeItem("token");
                goto("/");
            } else {
                alert("Fehler beim Verlassen des Teams.");
            }
        } catch (err) {
            console.error(err);
            alert("Netzwerkfehler beim Verlassen des Teams.");
        }
    }

    function getTeamIdFromToken(token: string): number {
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            const payload = JSON.parse(jsonPayload);
            
            for (const key of Object.keys(payload)) {
                if (key.toLowerCase().includes('teamid')) {
                    const val = parseInt(payload[key]);
                    if (!isNaN(val)) return val;
                }
            }
            return 0;
        } catch (e) {
            return 0;
        }
    }

    async function loadTeamMembersList() {
        if (currentTeamId > 0) {
            try {
                const members = await getTeamMembers(currentTeamId);
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

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5121/kanbanHub", { 
                accessTokenFactory: () => token,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveTaskUpdate", async (message) => {
            console.log("Live-Update empfangen:", message);
            await loadTasks();
        });

        connection.on("ReceiveUpdateUsername", async (message) => {
           console.log("Username-Update empfangen:", message);
           await loadTeamMembersList(); 
        });

        connection.on("UserJoined", async (message) => {
            console.log("Neues Mitglied:", message);
            await loadTeamMembersList();
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

    async function handleTeamAction(action: 'create' | 'join') {
        if (!teamName.trim() || !teamPassword.trim()) return;

        try {
            if (action === 'create') {
                const newToken = await registerTeam(teamName, teamPassword);
                if (newToken) {
                    localStorage.setItem("token", newToken);
                    currentTeamId = getTeamIdFromToken(newToken);
                }
                alert("Team erfolgreich erstellt!");
            } else if (action === 'join') {
                const newToken = await joinTeam(teamName, teamPassword);
                if (newToken) {
                    localStorage.setItem("token", newToken);
                    currentTeamId = getTeamIdFromToken(newToken);
                }
                alert("Team erfolgreich beigetreten!");
            }
            
            showTeamPopup = false;
            teamName = "";
            teamPassword = "";
            await loadTasks();
            await loadTeamMembersList();
            window.location.reload(); 
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

    async function handleDelete(taskId: number) {
        if (!confirm("Willst du diesen Task wirklich löschen?")) return;

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
    let completionPercentage = $derived(tasks.length > 0 ? Math.round((doneTasks.length / tasks.length) * 100) : 0);

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
            <div class="sidebar-brand">
                <span class="brand-icon"></span>
                <h2>FlowBoard</h2>
            </div>

            <div class="nav-section">
                <span class="sidebar-label">Navigation</span>
                <button type="button" class="nav-item active">📊 Projekt Board</button>
            </div>

            <div class="nav-section">
                <span class="sidebar-label">Workspace & Teams</span>
                <button type="button" class="btn-secondary" onclick={() => showTeamPopup = true}>👥 Team verwalten</button>
                
                <div class="team-status-box">
                    {#if currentTeamId > 0}
                        <div class="status-badge team">
                            <span class="pulse-dot"></span>
                            <span>Team #{currentTeamId} aktiv</span>
                        </div>
                    {:else}
                        <div class="status-badge private">
                            <span class="pulse-dot private-dot"></span>
                            <span>Privater Workspace</span>
                        </div>
                    {/if}
                </div>
            </div>

            <div class="sidebar-footer">
                <p class="app-version">v1.2.0 • Realtime Sync</p>
            </div>
        </aside>
        
        <!-- 2. ZENTRUM: Kanban Board -->
        <main class="kanban-main">
            <header class="board-header">
                <div class="header-title-wrapper">
                    <h1>Projekt Board</h1>
                    <span class="view-badge {currentTeamId > 0 ? 'team' : 'private'}">
                        {currentTeamId > 0 ? `Team #${currentTeamId}` : 'Privat'}
                    </span>
                </div>
                <p class="board-subtitle">
                    {currentTeamId > 0 ? 'Synchronisiert mit deinem Team in Echtzeit' : 'Deine persönlichen Aufgaben im Überblick'}
                </p>
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
                                <button type="button" class="btn-delete" onclick={() => handleDelete(task.id)} title="Task löschen">
                                    <span class="action-icon">X</span>
                                </button>
                                <button type="button" class="btn-arrow" onclick={() => moveTask(task, 'in-progress')} title="Verschieben nach In Progress">
                                    <span>In Bearbeitung</span>
                                    <span class="action-arrow-icon">→</span>
                                </button>
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
                                <div class="task-actions-left">
                                    <button type="button" class="btn-delete" onclick={() => handleDelete(task.id)} title="Task löschen">
                                        <span class="action-icon">X</span>
                                    </button>
                                    <button type="button" class="btn-arrow" onclick={() => moveTask(task, 'Todo')} title="Zurück zu Todo">
                                        <span class="action-arrow-icon">←</span>
                                    </button>
                                </div>
                                <button type="button" class="btn-arrow primary-move" onclick={() => moveTask(task, 'done')} title="Abschließen">
                                    <span>Erledigen</span>
                                    <span class="action-arrow-icon">→</span>
                                </button>
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
                                <button type="button" class="btn-delete" onclick={() => handleDelete(task.id)} title="Task löschen">
                                    <span class="action-icon">X</span>
                                </button>
                                <button type="button" class="btn-arrow" onclick={() => moveTask(task, 'in-progress')} title="Zurück in Bearbeitung">
                                    <span class="action-arrow-icon">←</span>
                                    <span>Zurück</span>
                                </button>
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
            <!-- Profil & Settings Header -->
            <div class="right-header-card">
                <div class="profile-info">
                    <div class="avatar-circle">⚡</div>
                    <div class="profile-text">
                        <span class="profile-name">Workspace User</span>
                        <span class="profile-status">Online</span>
                    </div>
                </div>
                <button type="button" class="btn-icon-settings" onclick={() => showSettingsPopup = true} title="Einstellungen">
                    ⚙️
                </button>
            </div>

            <!-- Schnell-Aktion Button -->
            <button type="button" class="btn-primary btn-glow" onclick={() => showCreateTaskPopup = true}>
                + Neuer Task
            </button>

            <!-- Statistik Widget -->
            <div class="widget-card stats-widget">
                <div class="widget-header">
                    <h4>Fortschritt</h4>
                    <span class="stats-percentage">{completionPercentage}%</span>
                </div>
                <div class="progress-bar-bg">
                    <div class="progress-bar-fill" style="width: {completionPercentage}%"></div>
                </div>
                <div class="stats-details">
                    <span>Erledigt: <strong>{doneTasks.length}</strong> von {tasks.length}</span>
                </div>
            </div>

            <!-- Team Mitglieder Widget -->
            {#if currentTeamId > 0}
                <div class="widget-card members-widget">
                    <div class="widget-header">
                        <h4>Team Mitglieder</h4>
                        <span class="member-count-badge">{teamMembers.length}</span>
                    </div>
                    <ul class="members-list">
                        {#each teamMembers as member}
                            <li class="member-item">
                                <div class="member-avatar">{member.charAt(0).toUpperCase()}</div>
                                <span class="member-name">{member}</span>
                            </li>
                        {:else}
                            <li class="empty-members">Keine weiteren Mitglieder im Team</li>
                        {/each}
                    </ul>

                    <button type="button" class="btn-leave-team" onclick={handleLeaveTeam}>
                        Team verlassen
                    </button>
                </div>
            {/if}
        </aside>
    </div>

    <!-- POPUP: Neuen Task erstellen -->
    {#if showCreateTaskPopup}
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <div class="modal-backdrop" role="button" tabindex="0" onclick={() => showCreateTaskPopup = false}>
            <!-- svelte-ignore a11y_click_events_have_key_events -->
            <!-- svelte-ignore a11y_no_static_element_interactions -->
            <div class="modal-content" role="presentation" onclick={(e) => e.stopPropagation()}>
                <div class="modal-header-modern">
                    <div class="modal-icon-badge">✨</div>
                    <h3>Neuen Task erstellen</h3>
                </div>
                <div class="form-group">
                    <label for="title">Titel</label>
                    <input id="title" type="text" bind:value={newTaskTitle} placeholder="z.B. API anbinden" />
                </div>
                <div class="form-group">
                    <label for="desc">Beschreibung</label>
                    <textarea id="desc" bind:value={newTaskDesc} placeholder="Kurze Beschreibung..." rows="3"></textarea>
                </div>
                <div class="modal-actions">
                    <button type="button" class="btn-close" onclick={() => showCreateTaskPopup = false}>Abbrechen</button>
                    <button type="button" class="btn-primary" onclick={handleCreateTask}>Erstellen</button>
                </div>
            </div>
        </div>
    {/if}

    <!-- POPUP: Team erstellen / beitreten -->
    {#if showTeamPopup}
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <div class="modal-backdrop" role="button" tabindex="0" onclick={() => showTeamPopup = false}>
            <!-- svelte-ignore a11y_click_events_have_key_events -->
            <!-- svelte-ignore a11y_no_static_element_interactions -->
            <div class="modal-content" role="presentation" onclick={(e) => e.stopPropagation()}>
                <div class="modal-header-modern">
                    <div class="modal-icon-badge">👥</div>
                    <h3>Team verwalten</h3>
                </div>
                <div class="form-group">
                    <label for="teamName">Team Name</label>
                    <input id="teamName" type="text" bind:value={teamName} placeholder="z.B. Entwickler-Team" />
                </div>
                <div class="form-group">
                    <label for="teamPass">Passwort</label>
                    <input id="teamPass" type="password" bind:value={teamPassword} placeholder="Geheimes Passwort..." />
                </div>
                <div class="modal-actions" style="flex-direction: column; gap: 0.5rem;">
                    <button type="button" class="btn-primary" onclick={() => handleTeamAction('create')} style="width: 100%;">
                        Team erstellen
                    </button>
                    <button type="button" class="btn-secondary" onclick={() => handleTeamAction('join')} style="width: 100%;">
                        Team beitreten
                    </button>
                    <button type="button" class="btn-close" onclick={() => showTeamPopup = false} style="width: 100%; margin-top: 0.5rem;">
                        Abbrechen
                    </button>
                </div>
            </div>
        </div>
    {/if}

    <!-- POPUP: Einstellungen & Logout -->
    {#if showSettingsPopup}
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <div class="modal-backdrop" role="button" tabindex="0" onclick={() => showSettingsPopup = false}>
            <!-- svelte-ignore a11y_click_events_have_key_events -->
            <!-- svelte-ignore a11y_no_static_element_interactions -->
            <div class="modal-content" role="presentation" onclick={(e) => e.stopPropagation()}>
                <div class="modal-header-modern">
                    <div class="modal-icon-badge">⚙️</div>
                    <h3>Einstellungen</h3>
                </div>
                <div class="form-group">
                    <label for="newUsername">Neuer Username</label>
                    <input id="newUsername" type="text" bind:value={newUsername} placeholder="Neuer Username" />
                </div>
                <div class="form-group">
                    <label for="oldPassword">Altes Passwort</label>
                    <input id="oldPassword" type="password" bind:value={oldPassword} placeholder="Altes Passwort" />
                </div>
                <div class="form-group">
                    <label for="newPassword">Neues Passwort</label>
                    <input id="newPassword" type="password" bind:value={newPassword} placeholder="Neues Passwort" />
                </div>
                <div class="modal-actions" style="flex-direction: column; gap: 0.5rem;">
                    <button type="button" class="btn-secondary" onclick={() => handleChangeUsername()} style="width: 100%;">Username ändern</button>
                    <button type="button" class="btn-secondary" onclick={() => handleChangePassword()} style="width: 100%;">Passwort ändern</button>
                    <button type="button" class="btn-logout" onclick={logout} style="width: 100%;">Ausloggen</button>
                    <button type="button" class="btn-close" onclick={() => showSettingsPopup = false} style="width: 100%; margin-top: 0.5rem;">Schließen</button>
                </div>
            </div>
        </div>
    {/if}
{/if}

<style>
    :global(html), :global(body) { 
        margin: 0; padding: 0; width: 100vw; height: 100vh; 
        background: linear-gradient(135deg, #022c22 0%, #064e3b 40%, #09090b 100%) !important; 
        color: #ffffff; font-family: system-ui, -apple-system, sans-serif; overflow-x: hidden; 
    }
    
    .loading-screen { background: #09090b; color: white; height: 100vh; display: flex; justify-content: center; align-items: center; }
    .dashboard-layout { display: grid; grid-template-columns: 260px 1fr 300px; height: 100vh; box-sizing: border-box; }
    
    .sidebar-left, .sidebar-right { 
        background-color: rgba(6, 78, 59, 0.25); 
        border: 1px solid rgba(16, 185, 129, 0.15); 
        padding: 1.5rem; display: flex; flex-direction: column; gap: 1.5rem; box-sizing: border-box; overflow-y: auto; 
    }
    .sidebar-left { border-left: none; border-top: none; border-bottom: none; }
    .sidebar-right { border-right: none; border-top: none; border-bottom: none; gap: 1.2rem; }
    
    /* LINKE SIDEBAR STYLING */
    .sidebar-brand { display: flex; align-items: center; gap: 0.75rem; padding-bottom: 0.5rem; border-bottom: 1px solid rgba(16, 185, 129, 0.15); }
    .sidebar-brand h2 { margin: 0; font-size: 1.2rem; font-weight: 700; letter-spacing: -0.025em; }
    .brand-icon { font-size: 1.2rem; }

    .nav-section { display: flex; flex-direction: column; gap: 0.75rem; }
    .sidebar-label { font-size: 0.75rem; text-transform: uppercase; color: #71717a; font-weight: 600; letter-spacing: 0.05em; }
    
    .nav-item {
        background: transparent; border: none; color: #a1a1aa; text-align: left; padding: 0.6rem 0.8rem; border-radius: 0.5rem; font-size: 0.9rem; cursor: pointer; transition: all 0.2s;
    }
    .nav-item:hover, .nav-item.active { background: rgba(16, 185, 129, 0.1); color: #34d399; font-weight: 500; }

    .team-status-box { margin-top: 0.5rem; }
    .status-badge {
        display: flex; align-items: center; gap: 0.5rem; font-size: 0.85rem; padding: 0.6rem 0.8rem; border-radius: 0.5rem; background: rgba(9, 9, 11, 0.4); border: 1px solid rgba(16, 185, 129, 0.2);
    }
    .status-badge.team { color: #34d399; }
    .status-badge.private { color: #a1a1aa; }

    .pulse-dot { width: 8px; height: 8px; background-color: #10b981; border-radius: 50%; box-shadow: 0 0 8px #10b981; }
    .pulse-dot.private-dot { background-color: #71717a; box-shadow: none; }

    .sidebar-footer { margin-top: auto; padding-top: 1rem; border-top: 1px solid rgba(16, 185, 129, 0.15); }
    .app-version { margin: 0; font-size: 0.75rem; color: #52525b; text-align: center; }

    /* RECHTE SIDEBAR */
    .right-header-card {
        background: rgba(9, 9, 11, 0.5); border: 1px solid rgba(16, 185, 129, 0.2); border-radius: 0.75rem; padding: 0.8rem 1rem; display: flex; align-items: center; justify-content: space-between;
    }
    .profile-info { display: flex; align-items: center; gap: 0.75rem; }
    .avatar-circle { width: 36px; height: 36px; background: linear-gradient(135deg, #059669, #10b981); border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 1rem; box-shadow: 0 0 10px rgba(16, 185, 129, 0.3); }
    .profile-text { display: flex; flex-direction: column; }
    .profile-name { font-size: 0.85rem; font-weight: 600; color: #fff; }
    .profile-status { font-size: 0.7rem; color: #34d399; }

    .btn-icon-settings { background: transparent; border: none; font-size: 1.1rem; cursor: pointer; padding: 0.3rem; border-radius: 0.4rem; transition: background 0.2s; }
    .btn-icon-settings:hover { background: rgba(255, 255, 255, 0.1); }

    .btn-glow { width: 100%; box-shadow: 0 4px 14px rgba(5, 150, 105, 0.3); font-weight: 600; }

    .widget-card {
        background: rgba(9, 9, 11, 0.4); border: 1px solid rgba(16, 185, 129, 0.2); border-radius: 0.75rem; padding: 1.1rem; display: flex; flex-direction: column; gap: 0.8rem;
    }
    .widget-header { display: flex; justify-content: space-between; align-items: center; }
    .widget-header h4 { margin: 0; font-size: 0.85rem; text-transform: uppercase; color: #a1a1aa; letter-spacing: 0.05em; font-weight: 600; }

    .stats-percentage { font-size: 0.85rem; font-weight: 700; color: #34d399; }
    .progress-bar-bg { width: 100%; height: 6px; background: #27272a; border-radius: 3px; overflow: hidden; }
    .progress-bar-fill { height: 100%; background: linear-gradient(90deg, #059669, #34d399); border-radius: 3px; transition: width 0.4s ease; }
    .stats-details { font-size: 0.8rem; color: #a1a1aa; }
    .stats-details strong { color: #fff; }

    .member-count-badge { font-size: 0.75rem; background: #18181b; padding: 0.1rem 0.5rem; border-radius: 1rem; color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }
    .members-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.5rem; max-height: 160px; overflow-y: auto; }
    .member-item { display: flex; align-items: center; gap: 0.75rem; background: rgba(24, 24, 27, 0.6); padding: 0.5rem 0.75rem; border-radius: 0.5rem; border: 1px solid #27272a; }
    .member-avatar { width: 24px; height: 24px; background: #047857; color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 0.75rem; font-weight: 700; }
    .member-name { font-size: 0.85rem; color: #e4e4e7; }
    .empty-members { color: #71717a; font-size: 0.8rem; text-align: center; font-style: italic; padding: 0.5rem 0; }

    .kanban-main { padding: 2.5rem; display: flex; flex-direction: column; gap: 1.5rem; overflow-y: auto; }
    
    .board-header { display: flex; flex-direction: column; align-items: center; text-align: center; gap: 0.3rem; margin-bottom: 0.5rem; }
    .header-title-wrapper { display: flex; align-items: center; justify-content: center; gap: 1rem; width: 100%; }
    .board-header h1 { margin: 0; font-size: 1.8rem; font-weight: 700; letter-spacing: -0.025em; }
    .board-subtitle { margin: 0; color: #a1a1aa; font-size: 0.9rem; text-align: center; }
    
    .view-badge {
        font-size: 0.75rem; font-weight: 600; padding: 0.2rem 0.6rem; border-radius: 20px; text-transform: uppercase; letter-spacing: 0.05em;
    }
    .view-badge.team { background: rgba(5, 150, 105, 0.3); color: #34d399; border: 1px solid rgba(5, 150, 105, 0.5); }
    .view-badge.private { background: rgba(113, 113, 122, 0.2); color: #d4d4d8; border: 1px solid rgba(113, 113, 122, 0.4); }

    .kanban-columns { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1.5rem; flex: 1; }
    
    .column { background-color: rgba(9, 9, 11, 0.4); border: 1px solid rgba(16, 185, 129, 0.2); border-radius: 0.85rem; padding: 1.2rem; display: flex; flex-direction: column; gap: 1rem; min-width: 200px; backdrop-filter: blur(8px); }
    .column h3 { margin: 0; font-size: 0.95rem; color: #a1a1aa; border-bottom: 1px solid rgba(16, 185, 129, 0.15); padding-bottom: 0.75rem; display: flex; justify-content: space-between; align-items: center; font-weight: 600; }
    .task-count { font-size: 0.75rem; background: #18181b; padding: 0.1rem 0.5rem; border-radius: 1rem; color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }

    /* TASK-KARTEN */
    .task-card {
        background: linear-gradient(145deg, #18181b 0%, #121215 100%); 
        border: 1px solid rgba(39, 39, 42, 0.8); 
        padding: 1.15rem; 
        border-radius: 0.75rem; 
        display: flex; 
        flex-direction: column; 
        gap: 1rem; 
        transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1); 
        min-width: 0; 
        word-break: break-word;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
        position: relative;
        overflow: hidden;
    }
    
    .task-card::before {
        content: '';
        position: absolute;
        top: 0; left: 0;
        width: 3px; height: 100%;
        background: linear-gradient(180deg, #059669, #34d399);
        opacity: 0.6;
        transition: opacity 0.2s;
    }

    .task-card:hover { 
        border-color: rgba(16, 185, 129, 0.5); 
        transform: translateY(-3px); 
        box-shadow: 0 8px 24px rgba(5, 150, 105, 0.2); 
    }
    
    .task-card:hover::before { opacity: 1; }
    
    .task-content h4 { margin: 0 0 0.4rem 0; color: #ffffff; font-size: 1rem; font-weight: 600; letter-spacing: -0.01em; overflow-wrap: break-word; }
    .task-content p { margin: 0; color: #a1a1aa; font-size: 0.85rem; line-height: 1.45; overflow-wrap: break-word; }

    /* KARTEN-ACTIONS */
    .task-actions { display: flex; justify-content: space-between; align-items: center; border-top: 1px solid rgba(39, 39, 42, 0.6); padding-top: 0.8rem; margin-top: auto; gap: 0.5rem; }
    .task-actions-left { display: flex; gap: 0.4rem; align-items: center; }

    .btn-arrow { 
        background: rgba(24, 24, 27, 0.8); 
        border: 1px solid rgba(16, 185, 129, 0.2); 
        color: #d4d4d8; 
        font-size: 0.8rem; font-weight: 500;
        cursor: pointer; padding: 0.4rem 0.7rem; border-radius: 0.4rem; 
        display: flex; align-items: center; gap: 0.35rem;
        transition: all 0.2s; 
    }
    
    .btn-arrow:hover { 
        background: rgba(16, 185, 129, 0.15); 
        color: #34d399; border-color: rgba(16, 185, 129, 0.4);
        transform: translateY(-1px);
    }
    
    .primary-move { background: rgba(5, 150, 105, 0.2); color: #34d399; border-color: rgba(5, 150, 105, 0.4); }
    .primary-move:hover { background: rgba(5, 150, 105, 0.35); color: #ffffff; }

    .action-arrow-icon { font-size: 0.9rem; font-weight: bold; }

    .btn-delete { 
        background: rgba(239, 68, 68, 0.08); 
        border: 1px solid rgba(239, 68, 68, 0.2); 
        border-radius: 0.4rem; cursor: pointer; padding: 0.4rem 0.5rem; 
        display: flex; align-items: center; justify-content: center;
        transition: all 0.2s; 
    }
    .btn-delete:hover { background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); }
    .action-icon { font-size: 0.85rem; }

    .empty-text { color: #52525b; font-size: 0.85rem; font-style: italic; text-align: center; margin-top: 1rem; }

    /* FORMULARE & MODALS */
    .form-group { display: flex; flex-direction: column; gap: 0.4rem; margin-bottom: 1rem; }
    .form-group label { font-size: 0.8rem; font-weight: 600; color: #a1a1aa; text-transform: uppercase; letter-spacing: 0.05em; }
    
    input, textarea {
        background: #09090b; border: 1px solid #27272a; color: #fff; padding: 0.75rem; border-radius: 0.5rem; font-size: 0.9rem; outline: none; transition: border-color 0.2s;
    }
    input:focus, textarea:focus { border-color: #10b981; }

    button { padding: 0.6rem 1rem; border: none; border-radius: 0.5rem; cursor: pointer; font-weight: 500; font-size: 0.9rem; transition: background 0.2s, filter 0.2s; }

    .btn-primary { background-color: #059669; color: white; width: 100%; }
    .btn-primary:hover { background-color: #047857; box-shadow: 0 0 12px rgba(5, 150, 105, 0.4); }

    .btn-secondary { background-color: rgba(24, 24, 27, 0.8); color: #f4f4f5; border: 1px solid rgba(16, 185, 129, 0.3); text-align: center; }
    .btn-secondary:hover { background-color: rgba(39, 39, 42, 0.9); border-color: #10b981; }

    .btn-logout { background-color: #ef4444; color: white; }
    .btn-logout:hover { background-color: #dc2626; }

    .btn-close { background-color: transparent; color: #a1a1aa; border: 1px solid #3f3f46; width: 100%; }
    .btn-close:hover { background-color: rgba(255,255,255,0.05); color: #fff; }

    .btn-leave-team {
        background-color: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.3); color: #fca5a5; width: 100%; padding: 0.5rem; border-radius: 0.5rem; cursor: pointer; font-weight: 500; margin-top: 0.5rem; transition: background-color 0.2s; font-size: 0.85rem;
    }
    .btn-leave-team:hover { background-color: rgba(239, 68, 68, 0.2); }

    .modal-backdrop { position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; background: rgba(0, 0, 0, 0.75); backdrop-filter: blur(4px); display: flex; justify-content: center; align-items: center; z-index: 1000; }
    .modal-content { background: #18181b; border: 1px solid rgba(16, 185, 129, 0.3); padding: 2rem; border-radius: 0.85rem; width: 100%; max-width: 400px; display: flex; flex-direction: column; gap: 1rem; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5); }
    
    .modal-header-modern { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.5rem; }
    .modal-icon-badge { width: 32px; height: 32px; background: rgba(16, 185, 129, 0.15); border: 1px solid rgba(16, 185, 129, 0.3); border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 0.9rem; }
    .modal-header-modern h3 { margin: 0; font-size: 1.1rem; color: #fff; }
    .modal-actions { display: flex; gap: 0.75rem; margin-top: 0.5rem; }
</style>