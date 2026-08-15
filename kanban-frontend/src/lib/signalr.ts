import * as signalR from "@microsoft/signalr";

export function createSignalRConnection(token: string) {
    return new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5121/kanbanHub", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();
}