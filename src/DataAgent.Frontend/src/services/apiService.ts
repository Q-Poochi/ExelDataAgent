const API_BASE_URL = 'http://localhost:5196/api';

export interface StartAnalysisOptions {
    fileUrl: string;
    prompt?: string;
    callbackUrl?: string; // Optional since backend might handle it, but good to have
}

export interface SendEmailOptions {
    recipientEmail: string;
    recipientName: string;
}

export const apiService = {
    /**
     * Uploads a file (CSV or Excel) to the backend.
     */
    async uploadFile(file: File): Promise<{ fileUrl: string, jobId: string }> {
        const formData = new FormData();
        formData.append('file', file);

        const response = await fetch(`${API_BASE_URL}/files/upload`, {
            method: 'POST',
            body: formData,
        });

        if (!response.ok) {
            const errText = await response.text();
            throw new Error(`Upload failed: ${errText}`);
        }

        return await response.json();
    },

    /**
     * Starts the analysis pipeline.
     */
    async startAnalysis(command: StartAnalysisOptions): Promise<{ jobId: string }> {
        const response = await fetch(`${API_BASE_URL}/analysis/start`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(command)
        });

        if (!response.ok) {
            const errText = await response.text();
            throw new Error(`Failed to start analysis: ${errText}`);
        }

        return await response.json();
    },

    /**
     * Gets the current job status manually (fallback or initial check).
     */
    async getJobStatus(jobId: string): Promise<any> {
        const response = await fetch(`${API_BASE_URL}/analysis/${jobId}/status`);
        if (!response.ok) {
            const errText = await response.text();
            throw new Error(`Failed to get status: ${errText}`);
        }
        return await response.json();
    },

    /**
     * Triggers the sending of the report email.
     */
    async sendEmail(jobId: string, options: SendEmailOptions): Promise<{ success: boolean; message: string }> {
        const response = await fetch(`${API_BASE_URL}/analysis/${jobId}/send-email`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(options)
        });

        if (!response.ok) {
            const errText = await response.text();
            throw new Error(`Failed to send email: ${errText}`);
        }

        return await response.json();
    }
};
