import * as signalR from "@microsoft/signalr";

const SIGNALR_URL = (import.meta as any).env?.VITE_SIGNALR_URL ?? "http://localhost:5121/kanbanHub";

export function createSignalRConnection(token: string) {
    return new signalR.HubConnectionBuilder()
        .withUrl(SIGNALR_URL, {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();
}