import React from 'react';
import type { JobUpdate } from '../hooks/useAnalysisSignalR';
import { CheckCircle2, Circle, Loader2, XCircle } from 'lucide-react';
import styles from './ProgressTimeline.module.css';

interface ProgressTimelineProps {
    jobUpdate: JobUpdate | null;
}

const STEPS = [
    { title: "Upload & Verify", key: "upload_verify" },
    { title: "Analyze Dataset Structure", key: "analyze_schema" },
    { title: "Query Claude/Gemini", key: "query_llm" },
    { title: "Generate Python Code", key: "generate_code" },
    { title: "Execute Chart Service", key: "execute_chart" },
    { title: "Compile Metrics", key: "compile_metrics" },
    { title: "Generate PDF Report", key: "generate_pdf" },
    { title: "Finalize Deliverable", key: "finalize" }
];

export const ProgressTimeline: React.FC<ProgressTimelineProps> = ({ jobUpdate }) => {
    if (!jobUpdate) return null;

    // Simulate step mapping based on progress (0-100)
    const currentStepIndex = Math.min(
        STEPS.length - 1,
        Math.floor((jobUpdate.progress / 100) * STEPS.length)
    );

    const isComplete = jobUpdate.status === 2;
    const isError = jobUpdate.status === 3;

    return (
        <div className={`card ${styles.timelineContainer}`}>
            <h2>3. Progress Timeline</h2>
            
            <div className={styles.progressBarContainer}>
                <div 
                    className={styles.progressBar} 
                    style={{ width: `${jobUpdate.progress}%`, backgroundColor: isError ? 'var(--error-color)' : 'var(--primary-green)' }}
                ></div>
            </div>

            <div className={styles.timeline}>
                {STEPS.map((step, index) => {
                    let statusClass = '';
                    let Icon = Circle;

                    if (isError && index === currentStepIndex) {
                        statusClass = styles.error;
                        Icon = XCircle;
                    } else if (isComplete || index < currentStepIndex) {
                        statusClass = styles.completed;
                        Icon = CheckCircle2;
                    } else if (index === currentStepIndex) {
                        statusClass = styles.active;
                        Icon = Loader2;
                    }

                    return (
                        <div key={index} className={`${styles.step} ${statusClass}`}>
                            <div className={styles.stepIcon}>
                                <Icon className={statusClass === styles.active ? 'animate-spin' : ''} size={20} />
                            </div>
                            <div className={styles.stepContent}>
                                <div className={styles.stepTitle}>
                                    Step {index + 1}: {step.title}
                                    {statusClass === styles.active && (
                                        <span className={styles.stepTime}>{new Date(jobUpdate.updatedAt).toLocaleTimeString()}</span>
                                    )}
                                    {statusClass === styles.completed && (
                                        <span className={styles.stepTime}>Done</span>
                                    )}
                                </div>
                                {statusClass === styles.active && (
                                    <div className={styles.stepMessage}>{jobUpdate.message}</div>
                                )}
                                {isError && index === currentStepIndex && (
                                    <div className={styles.stepMessage} style={{ color: 'var(--error-color)' }}>{jobUpdate.message}</div>
                                )}
                                
                                {index === 3 && (statusClass === styles.active || statusClass === styles.completed) && jobUpdate.message.includes("import ") && (
                                    <div className={styles.codeBlock}>
                                        {jobUpdate.message}
                                    </div>
                                )}
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};
