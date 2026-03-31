import { useEffect, useState, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

export interface JobUpdate {
    jobId: string;
    status: number; // 0=Pending, 1=Processing, 2=Completed, 3=Failed
    progress: number;
    currentStep: string;
    message: string;
    resultUrl?: string;
    updatedAt: string;
}

export const useAnalysisSignalR = (jobId: string | null) => {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [jobUpdate, setJobUpdate] = useState<JobUpdate | null>(null);
    const [emailSent, setEmailSent] = useState<any>(null);
    const retryTimeoutRef = useRef<any>(null);

    useEffect(() => {
        if (!jobId) {
            if (connection) {
                connection.stop();
                setConnection(null);
            }
            return;
        }

        const buildConnection = async () => {
            const newConnection = new signalR.HubConnectionBuilder()
                .withUrl("http://localhost:5196/hubs/analysis")
                .withAutomaticReconnect()
                .build();

            newConnection.on("ReceiveJobUpdate", (update: JobUpdate) => {
                console.log("ReceiveJobUpdate:", update);
                setJobUpdate(update);
            });

            newConnection.on("ReceiveEmailSent", (update: any) => {
                console.log("ReceiveEmailSent:", update);
                setEmailSent(update);
            });

            try {
                await newConnection.start();
                console.log("SignalR Connected.");
                
                // Join the group for this job
                await newConnection.invoke("JoinJobGroup", jobId);
                console.log(`Joined group for job: ${jobId}`);
                
                setConnection(newConnection);
            } catch (err) {
                console.error("SignalR Connection Error: ", err);
                retryTimeoutRef.current = setTimeout(buildConnection, 5000);
            }
        };

        buildConnection();

        return () => {
            if (retryTimeoutRef.current) clearTimeout(retryTimeoutRef.current);
            if (connection) {
                // leave group? The backend handles on disconnect mostly, but we can stop
                connection.stop();
            }
        };
    }, [jobId]);

    return { jobUpdate, emailSent, connectionState: connection?.state };
};
