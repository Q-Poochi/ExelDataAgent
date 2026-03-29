class AnalysisClient {
    constructor(hubUrl) {
        this.hubUrl = hubUrl || '/hubs/analysis';
        this.connection = null;
        this.jobId = null;
        this.onJobUpdateCallbacks = [];
        this.onProgressCallbacks = [];
    }

    async connectToJob(jobId) {
        if (!jobId) {
            console.error('[AnalysisClient] JobId is required to connect.');
            return;
        }

        this.jobId = jobId;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl)
            .withAutomaticReconnect([0, 2000, 10000, 30000]) // Exponential backoff: 0s, 2s, 10s, 30s
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Register handlers
        this.connection.on("ReceiveJobUpdate", (update) => {
            console.log("[AnalysisClient] Received update:", update);
            this.onJobUpdateCallbacks.forEach(cb => cb(update));
        });

        this.connection.on("ReceiveProgress", (id, percent, message) => {
            if(id === this.jobId) {
                console.log(`[AnalysisClient] Progress Update: ${percent}% - ${message}`);
                this.onProgressCallbacks.forEach(cb => cb(percent, message));
            }
        });

        // Event hooks
        this.connection.onreconnecting((error) => {
            console.warn(`[AnalysisClient] Reconnecting due to error: ${error}`);
        });

        this.connection.onreconnected((connectionId) => {
            console.log(`[AnalysisClient] Reconnected. Connection ID: ${connectionId}`);
            // Rejoin group after reconnect
            this.connection.invoke("JoinJobGroup", this.jobId).catch(err => console.error(err));
        });

        // Start connection
        try {
            await this.connection.start();
            console.log('[AnalysisClient] SignalR Connected.');
            // Join the specific job group
            await this.connection.invoke("JoinJobGroup", this.jobId);
            console.log(`[AnalysisClient] Joined tracking group for Job: ${this.jobId}`);
        } catch (err) {
            console.error('[AnalysisClient] Connection failed: ', err);
            setTimeout(() => this.connectToJob(jobId), 5000);
        }
    }

    async disconnectFromJob() {
        if (this.connection) {
            await this.connection.stop();
            console.log('[AnalysisClient] Disconnected.');
            this.connection = null;
            this.jobId = null;
        }
    }

    onJobUpdate(callback) {
        this.onJobUpdateCallbacks.push(callback);
    }

    onProgress(callback) {
        this.onProgressCallbacks.push(callback);
    }
}

// Export for usage in other scripts
window.AnalysisClient = AnalysisClient;
