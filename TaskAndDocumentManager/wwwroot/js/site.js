(() => {
    const app = document.getElementById("realtime-app");

    if (!app) {
        return;
    }

    const storageKey = "task-doc-manager.jwt";
    const apiBase = "/api";
    const reconnectRefreshDelayMs = 500;
    const eventRefreshDelayMs = 900;
    const periodicRefreshMs = 60_000;

    const state = {
        token: localStorage.getItem(storageKey) || "",
        profile: null,
        notifications: [],
        presenceEvents: [],
        notificationHub: null,
        realtimeHub: null,
        refreshTimer: null,
        intervalId: null
    };

    const elements = {
        authMessage: document.getElementById("auth-message"),
        jwtToken: document.getElementById("jwt-token"),
        loadProfileButton: document.getElementById("load-profile-button"),
        loginForm: document.getElementById("login-form"),
        logoutButton: document.getElementById("logout-button"),
        notificationConnectionBadge: document.getElementById("notification-connection-badge"),
        notificationCount: document.getElementById("notification-count"),
        notificationsList: document.getElementById("notifications-list"),
        presenceCount: document.getElementById("presence-count"),
        presenceFeed: document.getElementById("presence-feed"),
        profileGrid: document.getElementById("profile-grid"),
        realtimeDetail: document.getElementById("realtime-detail"),
        realtimeStatus: document.getElementById("realtime-status"),
        refreshNotificationsButton: document.getElementById("refresh-notifications-button"),
        registerForm: document.getElementById("register-form"),
        saveTokenButton: document.getElementById("save-token-button"),
        sessionDetail: document.getElementById("session-detail"),
        sessionStatus: document.getElementById("session-status"),
        syncDetail: document.getElementById("sync-detail"),
        syncStatus: document.getElementById("sync-status")
    };

    elements.jwtToken.value = state.token;

    elements.loginForm.addEventListener("submit", handleLoginSubmit);
    elements.registerForm.addEventListener("submit", handleRegisterSubmit);
    elements.logoutButton.addEventListener("click", logout);
    elements.saveTokenButton.addEventListener("click", saveTokenFromTextarea);
    elements.loadProfileButton.addEventListener("click", () => bootstrapAuthenticatedExperience("manual profile refresh"));
    elements.refreshNotificationsButton.addEventListener("click", () => refreshNotifications("manual refresh"));
    document.addEventListener("visibilitychange", () => {
        if (!document.hidden && state.token) {
            scheduleNotificationsRefresh("tab visible", reconnectRefreshDelayMs);
        }
    });

    render();

    if (state.token) {
        bootstrapAuthenticatedExperience("stored token");
    }

    async function handleLoginSubmit(event) {
        event.preventDefault();

        const formData = new FormData(event.currentTarget);
        const email = formData.get("email");
        const password = formData.get("password");

        setAuthMessage("Signing in...", "muted");

        try {
            const response = await fetchJson(`${apiBase}/auth/login`, {
                method: "POST",
                body: JSON.stringify({ email, password })
            });

            state.token = response.token;
            persistToken();
            event.currentTarget.reset();
            setAuthMessage("Session created. Realtime connections are starting now.", "success");
            await bootstrapAuthenticatedExperience("login");
        } catch (error) {
            setAuthMessage(error.message, "error");
        }
    }

    async function handleRegisterSubmit(event) {
        event.preventDefault();

        const formData = new FormData(event.currentTarget);
        const email = formData.get("email");
        const password = formData.get("password");

        setAuthMessage("Creating account...", "muted");

        try {
            await fetchJson(`${apiBase}/auth/register`, {
                method: "POST",
                body: JSON.stringify({ email, password })
            });

            setAuthMessage("Registration succeeded. Sign in to start realtime updates.", "success");
            event.currentTarget.reset();
        } catch (error) {
            setAuthMessage(error.message, "error");
        }
    }

    async function bootstrapAuthenticatedExperience(reason) {
        if (!state.token) {
            render();
            return;
        }

        try {
            await Promise.all([
                refreshProfile(reason),
                refreshNotifications(reason)
            ]);

            if (!state.token) {
                render();
                return;
            }

            await ensureRealtimeConnections();
            startPeriodicRefresh();
            render();
        } catch (error) {
            setAuthMessage(error.message, "error");
            render();
        }
    }

    async function refreshProfile(reason) {
        setSessionState("Validating session", `Loading current user from the API after ${reason}.`);

        try {
            state.profile = await fetchJson(`${apiBase}/auth/me`, {
                headers: createAuthHeaders()
            });

            setSessionState("Signed in", `${state.profile.email} is active. API confirmed this session.`);
            render();
        } catch (error) {
            if (error.status === 401) {
                logout("Your session is no longer valid.");
                return;
            }

            throw error;
        }
    }

    async function refreshNotifications(reason) {
        if (!state.token) {
            return;
        }

        setSyncState("Refreshing", `Pulling notifications from the API because of ${reason}.`);

        try {
            const notifications = await fetchJson(`${apiBase}/notifications`, {
                headers: createAuthHeaders()
            });

            state.notifications = notifications;
            setSyncState(
                "Synced",
                `${notifications.length} notifications loaded from the API at ${new Date().toLocaleTimeString()}.`
            );
            render();
        } catch (error) {
            if (error.status === 401) {
                logout("Your session expired while refreshing notifications.");
                return;
            }

            setSyncState("Refresh failed", error.message);
            render();
        }
    }

    async function ensureRealtimeConnections() {
        if (!window.signalR) {
            setRealtimeState("Unavailable", "The SignalR browser client did not load, so realtime updates are disabled.");
            render();
            return;
        }

        if (!state.notificationHub) {
            state.notificationHub = buildHubConnection("/hubs/notifications");
            wireNotificationHub(state.notificationHub);
        }

        if (!state.realtimeHub) {
            state.realtimeHub = buildHubConnection("/hubs/realtime");
            wireRealtimeHub(state.realtimeHub);
        }

        if (state.notificationHub.state === window.signalR.HubConnectionState.Disconnected) {
            await state.notificationHub.start();
        }

        if (state.realtimeHub.state === window.signalR.HubConnectionState.Disconnected) {
            await state.realtimeHub.start();
        }

        setRealtimeState("Live", "Hub subscriptions are active. The UI still re-syncs from the API after reconnects and events.");
        render();
    }

    function buildHubConnection(url) {
        return new window.signalR.HubConnectionBuilder()
            .withUrl(url, {
                accessTokenFactory: () => state.token
            })
            .withAutomaticReconnect()
            .build();
    }

    function wireNotificationHub(connection) {
        connection.on("NotificationCreated", notification => {
            mergeNotification(notification);
            setSyncState("Realtime update", "A notification arrived over SignalR. Scheduling an API refresh to reconcile.");
            render();
            scheduleNotificationsRefresh("notification event", eventRefreshDelayMs);
        });

        connection.onreconnecting(() => {
            setRealtimeState("Recovering", "Notification hub is reconnecting. API refresh will run when the connection returns.");
            updateNotificationHubBadge("Hub reconnecting", "warning");
            render();
        });

        connection.onreconnected(() => {
            updateNotificationHubBadge("Hub live", "success");
            setRealtimeState("Live", "Notification hub reconnected. Re-syncing from the API now.");
            render();
            scheduleNotificationsRefresh("notification hub reconnected", reconnectRefreshDelayMs);
        });

        connection.onclose(() => {
            updateNotificationHubBadge("Hub offline", "neutral");
            setRealtimeState("Offline", "Notification hub disconnected. API remains the source of truth until reconnect succeeds.");
            render();
        });
    }

    function wireRealtimeHub(connection) {
        connection.on("UserOnline", userId => {
            prependPresenceEvent(`User ${userId} came online.`);
            setRealtimeState("Presence update", "A presence event arrived over SignalR. The UI updated instantly.");
            render();
        });

        connection.on("UserOffline", userId => {
            prependPresenceEvent(`User ${userId} went offline.`);
            setRealtimeState("Presence update", "A presence event arrived over SignalR. The UI updated instantly.");
            render();
        });

        connection.onreconnecting(() => {
            setRealtimeState("Recovering", "Presence hub is reconnecting. Fresh data will come from the API after reconnection.");
            render();
        });

        connection.onreconnected(() => {
            setRealtimeState("Live", "Presence hub reconnected. Refreshing notifications from the API to avoid drift.");
            render();
            scheduleNotificationsRefresh("presence hub reconnected", reconnectRefreshDelayMs);
        });

        connection.onclose(() => {
            setRealtimeState("Offline", "Presence hub disconnected. Realtime may miss events until reconnect succeeds.");
            render();
        });
    }

    async function markNotificationAsRead(notificationId) {
        try {
            await fetchJson(`${apiBase}/notifications/${notificationId}/read`, {
                method: "PATCH",
                headers: createAuthHeaders()
            });

            state.notifications = state.notifications.map(notification =>
                notification.id === notificationId
                    ? { ...notification, isRead: true }
                    : notification);

            setSyncState("Synced", "Notification marked as read. Refreshing from the API to confirm persisted state.");
            render();
            scheduleNotificationsRefresh("mark as read", reconnectRefreshDelayMs);
        } catch (error) {
            if (error.status === 401) {
                logout("Your session expired while marking a notification as read.");
                return;
            }

            setAuthMessage(error.message, "error");
            render();
        }
    }

    function mergeNotification(notification) {
        state.notifications = [
            notification,
            ...state.notifications.filter(existing => existing.id !== notification.id)
        ];
    }

    function prependPresenceEvent(message) {
        state.presenceEvents = [
            {
                id: crypto.randomUUID(),
                message,
                timestamp: new Date().toLocaleTimeString()
            },
            ...state.presenceEvents
        ].slice(0, 8);
    }

    function scheduleNotificationsRefresh(reason, delay) {
        window.clearTimeout(state.refreshTimer);
        state.refreshTimer = window.setTimeout(() => {
            refreshNotifications(reason);
        }, delay);
    }

    function startPeriodicRefresh() {
        window.clearInterval(state.intervalId);
        state.intervalId = window.setInterval(() => {
            refreshNotifications("periodic health check");
        }, periodicRefreshMs);
    }

    function logout(message = "Signed out. Realtime subscriptions have been stopped.") {
        state.token = "";
        state.profile = null;
        state.notifications = [];
        state.presenceEvents = [];
        persistToken();
        stopRealtimeConnections();
        window.clearInterval(state.intervalId);
        window.clearTimeout(state.refreshTimer);
        setSessionState("Signed out", "Authenticate to connect to hubs.");
        setSyncState("Idle", "No refresh has happened yet.");
        setRealtimeState("Offline", "Waiting for an authenticated session.");
        setAuthMessage(message, "muted");
        render();
    }

    function stopRealtimeConnections() {
        const connections = [state.notificationHub, state.realtimeHub].filter(Boolean);

        for (const connection of connections) {
            connection.stop().catch(() => {
                // Nothing to do here; we are already tearing the session down.
            });
        }

        state.notificationHub = null;
        state.realtimeHub = null;
    }

    function saveTokenFromTextarea() {
        state.token = elements.jwtToken.value.trim();
        persistToken();

        if (!state.token) {
            logout("Stored token cleared.");
            return;
        }

        setAuthMessage("Token saved. Validating with the API now.", "success");
        bootstrapAuthenticatedExperience("manual token save");
    }

    function persistToken() {
        localStorage.setItem(storageKey, state.token);
        elements.jwtToken.value = state.token;
    }

    function setAuthMessage(message, tone) {
        elements.authMessage.textContent = message;
        elements.authMessage.dataset.tone = tone;
    }

    function setSessionState(status, detail) {
        elements.sessionStatus.textContent = status;
        elements.sessionDetail.textContent = detail;
    }

    function setSyncState(status, detail) {
        elements.syncStatus.textContent = status;
        elements.syncDetail.textContent = detail;
    }

    function setRealtimeState(status, detail) {
        elements.realtimeStatus.textContent = status;
        elements.realtimeDetail.textContent = detail;
    }

    function updateNotificationHubBadge(label, tone) {
        elements.notificationConnectionBadge.textContent = label;
        elements.notificationConnectionBadge.className = `status-pill ${tone}`;
    }

    function createAuthHeaders() {
        return {
            Authorization: `Bearer ${state.token}`
        };
    }

    async function fetchJson(url, options = {}) {
        const headers = {
            "Content-Type": "application/json",
            ...(options.headers || {})
        };

        const response = await fetch(url, {
            ...options,
            headers
        });

        const isJson = response.headers.get("content-type")?.includes("application/json");
        const payload = isJson ? await response.json() : null;

        if (!response.ok) {
            const error = new Error(payload?.message || `Request failed with status ${response.status}.`);
            error.status = response.status;
            throw error;
        }

        return payload;
    }

    function render() {
        elements.logoutButton.disabled = !state.token;
        elements.refreshNotificationsButton.disabled = !state.token;
        elements.loadProfileButton.disabled = !state.token;
        renderProfile();
        renderNotifications();
        renderPresenceFeed();
    }

    function renderProfile() {
        const fallbackValues = [
            "Not loaded",
            "Not loaded",
            "Unknown",
            "Unknown"
        ];

        const values = state.profile
            ? [
                state.profile.email,
                state.profile.role,
                state.profile.isActive ? "Active" : "Inactive",
                state.profile.id
            ]
            : fallbackValues;

        const rows = Array.from(elements.profileGrid.querySelectorAll("dd"));
        rows.forEach((row, index) => {
            row.textContent = values[index];
        });
    }

    function renderNotifications() {
        elements.notificationCount.textContent = `${state.notifications.length} notification${state.notifications.length === 1 ? "" : "s"}`;

        if (!state.notifications.length) {
            elements.notificationsList.innerHTML = '<li class="empty-state">No notifications yet. The API refresh loop is still active, so new server data will show up here.</li>';
            return;
        }

        elements.notificationsList.innerHTML = "";

        for (const notification of state.notifications) {
            const item = document.createElement("li");
            item.className = `notification-card ${notification.isRead ? "read" : "unread"}`;

            const title = document.createElement("div");
            title.className = "notification-card__title";
            title.textContent = notification.title;

            const message = document.createElement("p");
            message.className = "notification-card__message";
            message.textContent = notification.message;

            const footer = document.createElement("div");
            footer.className = "notification-card__footer";

            const meta = document.createElement("span");
            meta.textContent = new Date(notification.createdAtUtc).toLocaleString();

            footer.appendChild(meta);

            if (!notification.isRead) {
                const button = document.createElement("button");
                button.type = "button";
                button.className = "ghost-button small";
                button.textContent = "Mark as read";
                button.addEventListener("click", () => markNotificationAsRead(notification.id));
                footer.appendChild(button);
            }

            item.append(title, message, footer);
            elements.notificationsList.appendChild(item);
        }
    }

    function renderPresenceFeed() {
        elements.presenceCount.textContent = `${state.presenceEvents.length} event${state.presenceEvents.length === 1 ? "" : "s"}`;

        if (!state.presenceEvents.length) {
            elements.presenceFeed.innerHTML = '<li class="empty-state">Presence events will appear here after the realtime connection starts.</li>';
            return;
        }

        elements.presenceFeed.innerHTML = "";

        for (const event of state.presenceEvents) {
            const item = document.createElement("li");
            item.className = "event-item";
            item.innerHTML = `<strong>${event.timestamp}</strong><span>${event.message}</span>`;
            elements.presenceFeed.appendChild(item);
        }
    }
})();
