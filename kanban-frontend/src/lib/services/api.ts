const API_URL = "http://localhost:5121/api"; // Passe hier ggf. deinen Port an

function getAuthHeaders() {
    const token = localStorage.getItem("token");
    return {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${token}`
    };
}

// --- TASKS (entspricht KanbanTasksController) ---

// Holt alle Tasks des aktuell eingeloggten Users
export async function getMyTasks() {
    const res = await fetch(`${API_URL}/kanbantasks/user`, {
        headers: getAuthHeaders()
    });
    if (!res.ok) throw new Error("Fehler beim Laden der Tasks");
    return res.json();
}

// Holt Tasks für ein bestimmtes Team
export async function getTasksByTeam(teamId: number) {
    const res = await fetch(`${API_URL}/kanbantasks/team/${teamId}`, {
        headers: getAuthHeaders()
    });
    if (!res.ok) throw new Error("Fehler beim Laden der Team-Tasks");
    return res.json();
}

// Neuen Task erstellen (nutzt dein CanbanDto)
export async function createKanbanTask(title: string, description: string, status: string, teamId: number) {
    const res = await fetch(`${API_URL}/kanbantasks`, {
        method: "POST",
        headers: getAuthHeaders(),
        body: JSON.stringify({ title, description, status, teamId }) // Entspricht CanbanDto
    });
    if (!res.ok) throw new Error("Fehler beim Erstellen des Tasks");
    return res.json();
}

// Task aktualisieren (PUT)
export async function updateKanbanTask(id: number, title: string, description: string, status: string) {
    const res = await fetch(`${API_URL}/kanbantasks/${id}`, {
        method: "PUT",
        headers: getAuthHeaders(),
        body: JSON.stringify({ title, description, status })
    });
    if (!res.ok) throw new Error("Task konnte nicht aktualisiert werden");
    return res.json();
}

// Task löschen
export async function deleteKanbanTask(id: number) {
    const res = await fetch(`${API_URL}/kanbantasks/${id}`, {
        method: "DELETE",
        headers: getAuthHeaders()
    });
    if (!res.ok) throw new Error("Task konnte nicht gelöscht werden");
    return res.json();
}


// --- TEAMS (entspricht TeamsController) ---

// Team registrieren / erstellen
export async function registerTeam(name: string, password: string) {
    const res = await fetch(`${API_URL}/teams/register`, {
        method: "POST",
        headers: getAuthHeaders(), // Falls [Authorize] aktiv ist, ansonsten Headers weglassen
        body: JSON.stringify({ name, password }) // Entspricht TeamDto
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || "Fehler beim Erstellen des Teams");
    return data;
}

// Team beitreten (Login in ein Team) - Gibt direkt das neue JWT-Token mit der TeamId zurück!
export async function joinTeam(name: string, password: string) {
    const res = await fetch(`${API_URL}/teams/login`, {
        method: "POST",
        headers: getAuthHeaders(), // Braucht [Authorize] -> User muss eingeloggt sein!
        body: JSON.stringify({ name, password })
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || "Fehler beim Beitreten des Teams");
    
    // Gibt das Objekt zurück (enthält data.token und data.message)
    return data.token; 
}

// Team-Mitglieder für eine bestimmte TeamId abrufen
export async function getTeamMembers(teamId: number) {
    const res = await fetch(`${API_URL}/teams/members/${teamId}`, {
        headers: getAuthHeaders()
    });
    if (!res.ok) throw new Error("Fehler beim Laden der Team-Mitglieder");
    return res.json();
}

//Team verlassen
export async function leaveTeam(): Promise<boolean> {
    const res = await fetch(`${API_URL}/teams/leave`, {
        method: "POST",
        headers: getAuthHeaders(),
    });
    return res.ok;
}

//Change Password/Username 
export async function changePassword(oldPassword: string, newPassword: string) {
    const res = await fetch(`${API_URL}/auth/changepassword`, {
        method: "PUT",
        headers: getAuthHeaders(),
        body: JSON.stringify({ oldPassword, newPassword })
    });
    if (!res.ok) throw new Error("Fehler beim Ändern des Passworts");
    return res.json();
}

export async function changeUsername(newUsername: string) {
    const res = await fetch(`${API_URL}/auth/changeusername`, {
        method: "PUT",
        headers: getAuthHeaders(),
        body: JSON.stringify({ newUsername })
    });
    if (!res.ok) throw new Error("Fehler beim Ändern des Usernames");
    return res.json();
}
