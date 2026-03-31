import React, { useState } from 'react';
import { Download, Send, Loader2 } from 'lucide-react';
import { apiService } from '../services/apiService';
import type { JobUpdate } from '../hooks/useAnalysisSignalR';
import styles from './ResultActions.module.css';

interface ResultActionsProps {
    jobUpdate: JobUpdate | null;
}

export const ResultActions: React.FC<ResultActionsProps> = ({ jobUpdate }) => {
    const [email, setEmail] = useState('');
    const [name, setName] = useState('');
    const [sending, setSending] = useState(false);
    const [status, setStatus] = useState<{type: 'success' | 'error', msg: string} | null>(null);

    const isComplete = jobUpdate?.status === 2;
    const resultUrl = jobUpdate?.resultUrl;

    if (!isComplete || !resultUrl) return null;

    const handleSendEmail = async (e: React.FormEvent) => {
        e.preventDefault();
        setSending(true);
        setStatus(null);
        try {
            const res = await apiService.sendEmail(jobUpdate.jobId, { recipientEmail: email, recipientName: name });
            setStatus({ type: 'success', msg: res.message || 'Email đã được gửi thành công!' });
        } catch (error: any) {
            setStatus({ type: 'error', msg: error.message });
        } finally {
            setSending(false);
        }
    };

    return (
        <div className={`card ${styles.resultContainer}`}>
            <h2>4. Kết quả & Phân phối</h2>
            
            <div className={styles.successPanel}>
                <a href={resultUrl} target="_blank" rel="noopener noreferrer" className={styles.btnDownload}>
                    <Download size={24} /> Tải Báo Cáo Xuống (PDF)
                </a>

                <div className={styles.emailForm}>
                    <h3 style={{ marginBottom: '1rem', textAlign: 'center' }}>Gửi Báo Cáo Qua Email</h3>
                    <form onSubmit={handleSendEmail}>
                        <div className={styles.inputGroup}>
                            <label>Họ và Tên</label>
                            <input 
                                type="text" 
                                required 
                                value={name} 
                                onChange={(e) => setName(e.target.value)}
                                className={styles.inputField} 
                                placeholder="Nguyễn Văn A" 
                            />
                        </div>
                        <div className={styles.inputGroup}>
                            <label>Địa chỉ Email</label>
                            <input 
                                type="email" 
                                required 
                                value={email} 
                                onChange={(e) => setEmail(e.target.value)}
                                className={styles.inputField} 
                                placeholder="nguyenvana@example.com" 
                            />
                        </div>
                        <button type="submit" disabled={sending} className="button-primary btnSend">
                            {sending ? <Loader2 className="animate-spin" /> : <Send size={20} />}
                            {sending ? ' Đang gửi...' : ' Gửi báo cáo'}
                        </button>
                    </form>

                    {status && (
                        <div className={`${styles.statusMessage} ${status.type === 'success' ? styles.statusSuccess : styles.statusError}`}>
                            {status.msg}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};
