<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import { getMyTasks, getTasksByTeam, createKanbanTask, updateKanbanTask, deleteKanbanTask, registerTeam, joinTeam, getTeamMembers, leaveTeam, changePassword, changeUsername, kickTeamMember} from '$lib/services/api';
    import * as signalR from "@microsoft/signalr";
    import { version } from '../../../package.json';

    let connection: signalR.HubConnection | null = null;

    let isLoading: boolean = $state(true);
    let tasks: Task[] = $state([]);

    let currentUsername: string = $state("Workspace User");
    let currentTeamName: string = $state(typeof window !== 'undefined' ? localStorage.getItem("currentTeamName") || "" : "");
    
    // Popups
    let showTeamPopup: boolean = $state(false);
    let showSettingsPopup: boolean = $state(false);
    let showCreateTaskPopup: boolean = $state(false); 
    
    // Task Lösch-Modal State
    let showDeleteModal: boolean = $state(false);
    let taskToDeleteId: number | null = $state(null);
    
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

    // Toast State
    let toastMessage: string = $state("");
    let toastType: 'success' | 'error' = $state('success');
    let toastVisible: boolean = $state(false);
    let toastTimer: any = null;

    let kickingMemberId: number | null = $state(null);
    let confirmingLeave: boolean = $state(false);

    let teamMembers: { id: number; username: string; isAdmin: boolean }[] = $state([]);
    let isCurrentuserAdmin: boolean = $state(false);
    let currentUserId: number = $state(0);

    interface Task {
        id: number;
        title: string;
        description: string;
        status: 'Todo' | 'in-progress' | 'done';
    }

    function showToast(message: string, type: 'success' | 'error' = 'success') {
        toastMessage = message;
        toastType = type;
        toastVisible = true;
        if (toastTimer) clearTimeout(toastTimer);
        toastTimer = setTimeout(() => { toastVisible = false; }, 3500);
    }

    async function handleChangeUsername() {
        if (!newUsername.trim()) {
            showToast("Bitte gib einen neuen Benutzernamen ein.", 'error');
            return;
        }
        try {
            const message = await changeUsername(newUsername);
            showToast(message, 'success');

            currentUsername = newUsername;
            localStorage.setItem("username", newUsername);
            newUsername = "";
            showSettingsPopup = false;
        } catch (err) {
            console.error(err);
            showToast("Fehler beim Ändern des Benutzernamens.", 'error');
        }
    }

    async function handleChangePassword() {
        if (!oldPassword || !newPassword) {
            showToast("Bitte fülle alle Passwort-Felder aus.", 'error');
            return;
        }
        try {
            const message = await changePassword(oldPassword, newPassword);
            showToast(message, 'success');
            oldPassword = "";
            newPassword = "";
            showSettingsPopup = false;
        } catch (err) {
            console.error(err);
            showToast("Fehler beim Ändern des Passworts.", 'error');
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
        try {
            const success = await leaveTeam();
            if (success.token) {
                localStorage.setItem("token", success.token);
            }
            localStorage.removeItem("currentTeamName");
            currentTeamId = 0;

            if(connection){
                await connection.stop();
                await connection.start();
            }

            await loadTasks();
            showToast(success.message || "Team verlassen", 'success');
            //setTimeout(() => window.location.reload(), 1000);
        } catch (err: any) {
            console.error(err);
            showToast("Netzwerkfehler beim Verlassen des Teams.", 'error');
        }
    }

    async function handleKick(userIdToKick: number) {
        try {
            await kickTeamMember(currentTeamId, userIdToKick); 
            showToast("Mitglied erfolgreich gekickt.", 'success');
            kickingMemberId = null;
            await loadTeamMembersList();
        } catch (err: any) {
            showToast(err.message || "Fehler beim Kicken.", 'error');
        }
    }

    function getTeamIdFromToken(token: string): number {
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join(''));
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

    function getUserIdFromToken(token: string): number {
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join(''));
            const payload = JSON.parse(jsonPayload);
            for (const key of Object.keys(payload)) {
                if (key.toLowerCase().includes('nameid') || key.toLowerCase() === 'sub' || key.toLowerCase() === 'id') {
                    const val = parseInt(payload[key]);
                    if (!isNaN(val)) return val;
                }
            }
            return 0;
        } catch (e) {
            return 0;
        }
    }

    function getUsernameFromToken(token: string): string {
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join(''));
            const payload = JSON.parse(jsonPayload);
            for (const key of Object.keys(payload)) {
                if (key.toLowerCase().includes('unique_name') || key.toLowerCase().includes('username') || key.toLowerCase().includes('name')) {
                    return payload[key];
                }
            }
            return "Workspace User";
        } catch (e) {
            return "Workspace User";
        }
    }

    async function loadTeamMembersList() {
        if (currentTeamId > 0) {
            try {
                const members = await getTeamMembers(currentTeamId);
                teamMembers = members;
                const me = members.find((m: any) => m.id === currentUserId || m.Id === currentUserId);
                isCurrentuserAdmin = me ? (me.isAdmin ?? me.IsAdmin ?? false) : false;

            } catch (err) {
                console.error("Fehler beim Laden der Team-Mitglieder:", err);
                teamMembers = [];
                isCurrentuserAdmin = false;
            }
        } else {
            teamMembers = [];
            isCurrentuserAdmin = false;
        }
    }

    onMount(async () => {
        const token = localStorage.getItem("token");
        if (!token) {
            goto("/"); 
            return;
        }

        currentTeamId = getTeamIdFromToken(token);
        currentUserId = getUserIdFromToken(token);
        
        const savedUsername = localStorage.getItem("username");
        if (savedUsername) {
            currentUsername = savedUsername;
        } else {
            currentUsername = getUsernameFromToken(token);
            localStorage.setItem("username", currentUsername);
        }

        await loadTasks();
        await loadTeamMembersList();
        isLoading = false;

        connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5121/kanbanHub", { 
                accessTokenFactory: () => localStorage.getItem("token") || "",
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveTaskUpdate", async () => { await loadTasks(); });
        connection.on("ReceiveUpdateUsername", async () => { await loadTeamMembersList(); });
        connection.on("UserJoined", async () => { await loadTeamMembersList(); });

        connection.on("YouWereKicked", async (kickedUserId: number) => {
            if (kickedUserId === currentUserId) {
                showToast("Du wurdest aus dem Team geworfen!", 'error');
                
                //Für Zukunft: Backend soll direkt neues Token schicken
                currentTeamId = 0;
                localStorage.removeItem("currentTeamName");
                
                if (connection) {
                    await connection.stop();
                    await connection.start();
                }

                await loadTasks();
                await loadTeamMembersList();
            } else {
                await loadTeamMembersList();
            }
        });

        try {
            await connection.start();
        } catch (err) {
            console.error("SignalR Verbindungsfehler: ", err);
        }
    });

    async function handleCreateTask() {
        if (!newTaskTitle.trim()) {
            showToast("Bitte gib einen Titel für den Task ein.", 'error');
            return; 
        }
        try {
            await createKanbanTask(newTaskTitle, newTaskDesc, 'Todo', currentTeamId);
            showCreateTaskPopup = false;
            newTaskTitle = "";
            newTaskDesc = "";
            showToast("Task erfolgreich erstellt!", 'success');
            await loadTasks();
        } catch (err) {
            console.error(err);
            showToast("Fehler beim Erstellen des Tasks.", 'error');
        }
    }

    async function handleTeamAction(action: 'create' | 'join') {
        if (!teamName.trim() || !teamPassword.trim()) return;
        try {
            let response;
            if (action === 'create') {
                response = await registerTeam(teamName, teamPassword);
                showToast("Team erfolgreich erstellt!", 'success');
            } else {
                response = await joinTeam(teamName, teamPassword);
                showToast("Team erfolgreich beigetreten!", 'success');
            }

            if (response && response.token) {
                localStorage.setItem("token", response.token);
                currentTeamId = getTeamIdFromToken(response.token);
                currentTeamName = teamName;
            }

            localStorage.setItem("currentTeamName", teamName);
            
            showTeamPopup = false;
            teamName = "";
            teamPassword = "";

            if(connection){
                await connection.stop();
                await connection.start();
            }
            await loadTasks();
            await loadTeamMembersList();
            //setTimeout(() => window.location.reload(), 1000);
        } catch (err) {
            console.error(err);
            showToast(`Fehler beim ${action === 'create' ? 'Erstellen' : 'Beitreten'}.`, 'error');
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
            showToast("Fehler beim Verschieben.", 'error');
        }
    }

    function confirmDelete(taskId: number) {
        taskToDeleteId = taskId;
        showDeleteModal = true;
    }

    async function executeDelete() {
        if (taskToDeleteId === null) return;
        try {
            await deleteKanbanTask(taskToDeleteId);
            tasks = tasks.filter(t => t.id !== taskToDeleteId);
            showToast("Task erfolgreich gelöscht.", 'success');
        } catch (error) {
            console.error(error);
            showToast("Fehler beim Löschen des Tasks.", 'error');
        } finally {
            showDeleteModal = false;
            taskToDeleteId = null;
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

    {#if toastVisible}
        <div class="toast-notification {toastType}">
            <span class="toast-icon">
                {#if toastType === 'success'}
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
                {:else}
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>
                {/if}
            </span>
            <span class="toast-text">{toastMessage}</span>
        </div>
    {/if}

    <div class="dashboard-layout">
        
        <!-- 1. LINKE SIDEBAR -->
        <aside class="sidebar-left">
            <div class="sidebar-brand">
                <div class="brand-logo-box">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon></svg>
                </div>
                <h2>FlowBoard</h2>
            </div>

            <div class="nav-section">
                <span class="sidebar-label">Navigation</span>
                <button type="button" class="nav-item active">
                    <span class="nav-icon">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7"></rect><rect x="14" y="3" width="7" height="7"></rect><rect x="14" y="14" width="7" height="7"></rect><rect x="3" y="14" width="7" height="7"></rect></svg>
                    </span> 
                    Projekt Board
                </button>
            </div>

            <div class="nav-section">
                <span class="sidebar-label">Workspace & Teams</span>
                <button type="button" class="btn-secondary" onclick={() => showTeamPopup = true}>
                    <span class="btn-icon">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle><path d="M23 21v-2a4 4 0 0 0-3-3.87"></path><path d="M16 3.13a4 4 0 0 1 0 7.75"></path></svg>
                    </span> 
                    Team verwalten
                </button>
                
                <div class="team-status-box">
                    {#if currentTeamId > 0}
                        <div class="status-badge team">
                            <span class="pulse-dot"></span>
                            <span>{currentTeamName ? currentTeamName : `Team #${currentTeamId}`} aktiv</span>
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
                <p class="app-version">v {version} • Realtime Sync</p>
            </div>
        </aside>
        
        <!-- 2. ZENTRUM: Kanban Board -->
        <main class="kanban-main">
            <header class="board-header">
                <div class="header-title-wrapper">
                    <h1>Projekt Board</h1>
                    <span class="view-badge {currentTeamId > 0 ? 'team' : 'private'}">
                       {currentTeamId > 0 ? (currentTeamName || `Team #${currentTeamId}`) : 'Privat'}
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
                                <button type="button" class="btn-delete" onclick={() => confirmDelete(task.id)} title="Task löschen">
                                    <span class="action-icon">
                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                                    </span>
                                </button>
                                <button type="button" class="btn-arrow" onclick={() => moveTask(task, 'in-progress')} title="Verschieben nach In Progress">
                                    <span>In Bearbeitung</span>
                                    <span class="action-arrow-icon">
                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"></line><polyline points="12 5 19 12 12 19"></polyline></svg>
                                    </span>
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
                                    <button type="button" class="btn-delete" onclick={() => confirmDelete(task.id)} title="Task löschen">
                                        <span class="action-icon">
                                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                                        </span>
                                    </button>
                                    <button type="button" class="btn-arrow" onclick={() => moveTask(task, 'Todo')} title="Zurück zu Todo">
                                        <span class="action-arrow-icon">
                                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>
                                        </span>
                                    </button>
                                </div>
                                <button type="button" class="btn-arrow primary-move" onclick={() => moveTask(task, 'done')} title="Abschließen">
                                    <span>Erledigen</span>
                                    <span class="action-arrow-icon">
                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"></line><polyline points="12 5 19 12 12 19"></polyline></svg>
                                    </span>
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
                                <button type="button" class="btn-delete" onclick={() => confirmDelete(task.id)} title="Task löschen">
                                    <span class="action-icon">
                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                                    </span>
                                </button>
                                <button type="button" class="btn-arrow" onclick={() => moveTask(task, 'in-progress')} title="Zurück in Bearbeitung">
                                    <span class="action-arrow-icon">
                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>
                                    </span>
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
                    <div class="avatar-circle">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                    </div>
                    <div class="profile-text">
                        <span class="profile-name">{currentUsername}</span>
                        <span class="profile-status">Online</span>
                    </div>
                </div>
                <button type="button" class="btn-icon-settings" onclick={() => showSettingsPopup = true} title="Einstellungen">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"></circle><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path></svg>
                </button>
            </div>

            <!-- Schnell-Aktion Button -->
            <button type="button" class="btn-primary btn-glow" onclick={() => showCreateTaskPopup = true}>
                <span style="display: inline-flex; vertical-align: middle; margin-right: 4px;">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
                </span>
                Neuer Task
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
                                <div class="member-avatar">{member.username.charAt(0).toUpperCase()}</div>
                                <span class="member-name">{member.username} {member.isAdmin ? '(Admin)' : ''}</span>

                                {#if isCurrentuserAdmin && member.id !== currentUserId}
                                    {#if kickingMemberId === member.id}
                                        <div style="display: flex; gap: 0.2rem;">
                                            <button type="button" class="btn-yes" onclick={() => handleKick(member.id)}>Ja</button>
                                            <button type="button" class="btn-no" onclick={() => kickingMemberId = null}>
                                                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                                            </button>
                                        </div>
                                    {:else}
                                        <button type="button" class="btn-kick" onclick={() => kickingMemberId = member.id} title="Mitglied kicken">
                                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                                        </button>
                                    {/if}
                                {/if}
                            </li>
                        {:else}
                            <li class="empty-members">Keine weiteren Mitglieder im Team</li>
                        {/each}
                    </ul>

                    {#if confirmingLeave}
                        <div style="display: flex; gap: 0.5rem; margin-top: 0.5rem;">
                            <button type="button" class="btn-logout" style="flex: 1; font-size: 0.8rem;" onclick={handleLeaveTeam}>Wirklich?</button>
                            <button type="button" class="btn-close" style="width: auto;" onclick={() => confirmingLeave = false}>
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                            </button>
                        </div>
                    {:else}
                        <button type="button" class="btn-leave-team" onclick={() => confirmingLeave = true}>
                            Team verlassen
                        </button>
                    {/if}
                </div>
            {/if}
        </aside>
    </div>

    <!-- POPUP: Task Löschen bestätigen -->
    {#if showDeleteModal}
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <div class="modal-backdrop" role="button" tabindex="0" onclick={() => showDeleteModal = false}>
            <!-- svelte-ignore a11y_click_events_have_key_events -->
            <!-- svelte-ignore a11y_no_static_element_interactions -->
            <div class="modal-content" role="presentation" onclick={(e) => e.stopPropagation()}>
                <div class="modal-header-modern">
                    <div class="modal-icon-badge warning-badge">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>
                    </div>
                    <h3>Task löschen?</h3>
                </div>
                <p style="color: #a1a1aa; font-size: 0.9rem; margin: 0;">Möchtest du diesen Task wirklich unwiderruflich löschen?</p>
                <div class="modal-actions" style="margin-top: 1rem;">
                    <button type="button" class="btn-close" onclick={() => showDeleteModal = false}>Abbrechen</button>
                    <button type="button" class="btn-logout" onclick={executeDelete}>Löschen</button>
                </div>
            </div>
        </div>
    {/if}

    <!-- POPUP: Neuen Task erstellen -->
    {#if showCreateTaskPopup}
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <div class="modal-backdrop" role="button" tabindex="0" onclick={() => showCreateTaskPopup = false}>
            <!-- svelte-ignore a11y_click_events_have_key_events -->
            <!-- svelte-ignore a11y_no_static_element_interactions -->
            <div class="modal-content" role="presentation" onclick={(e) => e.stopPropagation()}>
                <div class="modal-header-modern">
                    <div class="modal-icon-badge">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                    </div>
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
                    <div class="modal-icon-badge">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle><path d="M23 21v-2a4 4 0 0 0-3-3.87"></path><path d="M16 3.13a4 4 0 0 1 0 7.75"></path></svg>
                    </div>
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
                    <div class="modal-icon-badge">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"></circle><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path></svg>
                    </div>
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
    :global(html),
:global(body) {
    margin: 0;
    padding: 0;
    min-height: 100%;
    background: #09090b;
    color: #ffffff;
    color-scheme: dark;
    font-family: system-ui, -apple-system, sans-serif;
    overflow-x: hidden;
}

.loading-screen,
.dashboard-layout {
    min-height: 100dvh;
    background: linear-gradient(
        135deg,
        #022c22 0%,
        #064e3b 40%,
        #09090b 100%
    );
}

.loading-screen {
    color: white;
    display: flex;
    justify-content: center;
    align-items: center;
}

.dashboard-layout {
    display: grid;
    grid-template-columns: 260px 1fr 300px;
    box-sizing: border-box;
}
    
    .sidebar-left, .sidebar-right { 
        background-color: rgba(6, 78, 59, 0.25); 
        border: 1px solid rgba(16, 185, 129, 0.15); 
        padding: 1.5rem; display: flex; flex-direction: column; gap: 1.5rem; box-sizing: border-box; overflow-y: auto; 
    }
    .sidebar-left { border-left: none; border-top: none; border-bottom: none; }
    .sidebar-right { border-right: none; border-top: none; border-bottom: none; gap: 1.2rem; }
    
    .sidebar-brand { display: flex; align-items: center; gap: 0.75rem; padding-bottom: 0.5rem; border-bottom: 1px solid rgba(16, 185, 129, 0.15); }
    .sidebar-brand h2 { margin: 0; font-size: 1.2rem; font-weight: 700; letter-spacing: -0.025em; }
    .brand-logo-box { width: 30px; height: 30px; background: rgba(16, 185, 129, 0.2); border: 1px solid rgba(16, 185, 129, 0.4); border-radius: 6px; display: flex; align-items: center; justify-content: center; color: #34d399; }

    .nav-section { display: flex; flex-direction: column; gap: 0.75rem; }
    .sidebar-label { font-size: 0.75rem; text-transform: uppercase; color: #71717a; font-weight: 600; letter-spacing: 0.05em; }
    
    .nav-item {
        background: transparent; border: none; color: #a1a1aa; text-align: left; padding: 0.6rem 0.8rem; border-radius: 0.5rem; font-size: 0.9rem; cursor: pointer; transition: all 0.2s;
        display: flex;
        align-items: center;
        gap: 0.6rem;
    }
    .nav-item:hover, .nav-item.active { background: rgba(16, 185, 129, 0.1); color: #34d399; font-weight: 500; }

    .nav-icon, .btn-icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        vertical-align: middle;
        position: relative;
        top: -1px; 
    }

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
    .avatar-circle { width: 36px; height: 36px; background: linear-gradient(135deg, #059669, #10b981); color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 1rem; box-shadow: 0 0 10px rgba(16, 185, 129, 0.3); }
    .profile-text { display: flex; flex-direction: column; }
    .profile-name { font-size: 0.85rem; font-weight: 600; color: #fff; }
    .profile-status { font-size: 0.7rem; color: #34d399; }

    .btn-icon-settings { background: transparent; border: none; color: #a1a1aa; cursor: pointer; padding: 0.4rem; border-radius: 0.4rem; transition: background 0.2s, color 0.2s; display: inline-flex; align-items: center; justify-content: center; }
    .btn-icon-settings:hover { background: rgba(255, 255, 255, 0.1); color: #fff; }

    .btn-glow { width: 100%; box-shadow: 0 4px 14px rgba(5, 150, 105, 0.3); font-weight: 600; display: inline-flex; align-items: center; justify-content: center; }

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
    .member-name { font-size: 0.85rem; color: #e4e4e7; flex: 1; }
    .empty-members { color: #71717a; font-size: 0.8rem; text-align: center; font-style: italic; padding: 0.5rem 0; }
    
    .btn-kick {
        background: transparent; border: none; color: #a1a1aa; cursor: pointer; display: inline-flex; align-items: center; justify-content: center; padding: 0.2rem; border-radius: 4px; transition: color 0.2s, background 0.2s;
    }
    .btn-kick:hover { color: #f87171; background: rgba(239, 68, 68, 0.1); }

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
    
    /* HIER GEÄNDERT: min-width von 200px auf 300px erhöht für dickere/breitere Spalten */
    .column { background-color: rgba(9, 9, 11, 0.4); border: 1px solid rgba(16, 185, 129, 0.2); border-radius: 0.85rem; padding: 1.2rem; display: flex; flex-direction: column; gap: 1rem; min-width: 270px; backdrop-filter: blur(8px); }
    
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

    .action-arrow-icon { display: inline-flex; align-items: center; justify-content: center; vertical-align: middle; }

    .btn-delete { 
        background: rgba(239, 68, 68, 0.08); 
        border: 1px solid rgba(239, 68, 68, 0.2); 
        border-radius: 0.4rem; cursor: pointer; padding: 0.4rem 0.6rem; 
        display: flex; align-items: center; justify-content: center;
        color: #f87171;
        transition: all 0.2s; 
    }
    .btn-delete:hover { background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); }
    .action-icon { display: inline-flex; align-items: center; justify-content: center; }

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

    .btn-secondary { 
        background-color: rgba(24, 24, 27, 0.8); 
        color: #f4f4f5; 
        border: 1px solid rgba(16, 185, 129, 0.3); 
        text-align: left; 
        display: flex;
        align-items: center;
        gap: 0.6rem;
    }
    .btn-secondary:hover { background-color: rgba(39, 39, 42, 0.9); border-color: #10b981; }

    .btn-logout { background-color: #ef4444; color: white; }
    .btn-logout:hover { background-color: #dc2626; }

    .btn-close { background-color: transparent; color: #a1a1aa; border: 1px solid #3f3f46; width: 100%; display: inline-flex; align-items: center; justify-content: center; }
    .btn-close:hover { background-color: rgba(255,255,255,0.05); color: #fff; }

    .btn-leave-team {
        background-color: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.3); color: #fca5a5; width: 100%; padding: 0.5rem; border-radius: 0.5rem; cursor: pointer; font-weight: 500; margin-top: 0.5rem; transition: background-color 0.2s; font-size: 0.85rem;
    }
    .btn-leave-team:hover { background-color: rgba(239, 68, 68, 0.2); }

    .modal-backdrop { position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; background: rgba(0, 0, 0, 0.75); backdrop-filter: blur(4px); display: flex; justify-content: center; align-items: center; z-index: 1000; }
    .modal-content { background: #18181b; border: 1px solid rgba(16, 185, 129, 0.3); padding: 2rem; border-radius: 0.85rem; width: 100%; max-width: 400px; display: flex; flex-direction: column; gap: 1rem; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5); }
    
    .modal-header-modern { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.5rem; }
    .modal-icon-badge { width: 32px; height: 32px; background: rgba(16, 185, 129, 0.15); border: 1px solid rgba(16, 185, 129, 0.3); border-radius: 50%; display: flex; align-items: center; justify-content: center; color: #34d399; }
    .warning-badge { background: rgba(239, 68, 68, 0.15) !important; border-color: rgba(239, 68, 68, 0.3) !important; color: #f87171 !important; }
    .modal-header-modern h3 { margin: 0; font-size: 1.1rem; color: #fff; }
    .modal-actions { display: flex; gap: 0.75rem; margin-top: 0.5rem; }

    .toast-notification {
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.85rem 1.25rem;
        border-radius: 0.75rem;
        font-size: 0.9rem;
        font-weight: 500;
        box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.4);
        animation: slideIn 0.3s cubic-bezier(0.16, 1, 0.3, 1) forwards;
        backdrop-filter: blur(8px);
    }
    .toast-notification.success {
        background: rgba(6, 95, 70, 0.9);
        border: 1px solid rgba(52, 211, 153, 0.4);
        color: #34d399;
    }
    .toast-notification.error {
        background: rgba(127, 29, 29, 0.9);
        border: 1px solid rgba(248, 113, 113, 0.4);
        color: #f87171;
    }
    @keyframes slideIn {
        from { opacity: 0; transform: translateY(-20px) scale(0.95); }
        to { opacity: 1; transform: translateY(0) scale(1); }
    }

    .btn-yes { background: #059669; color: white; border: none; padding: 0.2rem 0.5rem; border-radius: 4px; cursor: pointer; font-weight: bold; font-size: 0.75rem; }
    .btn-no { background: #dc2626; color: white; border: none; padding: 0.2rem 0.5rem; border-radius: 4px; cursor: pointer; display: inline-flex; align-items: center; justify-content: center; }
</style>