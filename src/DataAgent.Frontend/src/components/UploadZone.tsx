import React, { useState, useRef } from 'react';
import { UploadCloud, FileSpreadsheet } from 'lucide-react';
import styles from './UploadZone.module.css';

interface UploadZoneProps {
    onFileSelect: (file: File, prompt: string) => void;
    isUploading: boolean;
}

const MAX_FILE_SIZE_MB = 10;
const ALLOWED_TYPES = [
    'text/csv',
    'application/vnd.ms-excel',
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
];

export const UploadZone: React.FC<UploadZoneProps> = ({ onFileSelect, isUploading }) => {
    const [file, setFile] = useState<File | null>(null);
    const [prompt, setPrompt] = useState('Phân tích dữ liệu này và cho tôi biết các xu hướng chính.');
    const [dragActive, setDragActive] = useState(false);
    const [error, setError] = useState('');
    const fileInputRef = useRef<HTMLInputElement>(null);

    const handleDrag = (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        if (e.type === 'dragenter' || e.type === 'dragover') {
            setDragActive(true);
        } else if (e.type === 'dragleave') {
            setDragActive(false);
        }
    };

    const validateFile = (selectedFile: File) => {
        setError('');
        if (!ALLOWED_TYPES.includes(selectedFile.type) && !selectedFile.name.match(/\.(csv|xls|xlsx)$/i)) {
            setError('Chỉ chấp nhận file .csv, .xls, .xlsx');
            return false;
        }
        if (selectedFile.size > MAX_FILE_SIZE_MB * 1024 * 1024) {
            setError(`Chỉ chấp nhận file tối đa ${MAX_FILE_SIZE_MB}MB.`);
            return false;
        }
        return true;
    };

    const handleDrop = (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setDragActive(false);
        if (e.dataTransfer.files && e.dataTransfer.files[0]) {
            const selectedFile = e.dataTransfer.files[0];
            if (validateFile(selectedFile)) {
                setFile(selectedFile);
            }
        }
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        e.preventDefault();
        if (e.target.files && e.target.files[0]) {
            const selectedFile = e.target.files[0];
            if (validateFile(selectedFile)) {
                setFile(selectedFile);
            }
        }
    };

    const handleClick = () => {
        fileInputRef.current?.click();
    };

    const handleSubmit = () => {
        if (file) {
            onFileSelect(file, prompt);
        }
    };

    const loadSampleData = async () => {
        // Create a dummy CSV file for demonstration
        const csvContent = "Mã Sản Phẩm,Tên Sản Phẩm,Doanh Thu,Chi Phí,Tỷ Suất Lợi Nhuận,Ngày Bán\nSP01,Laptop Dell,15000000,12000000,0.2,2023-10-01\nSP02,Chuột Logitech,500000,300000,0.4,2023-10-02\nSP03,Bàn Phím Cơ,1200000,800000,0.33,2023-10-03\nSP04,Màn Hình LG,4500000,3500000,0.22,2023-10-04\nSP05,Tai Nghe Sony,2500000,1800000,0.28,2023-10-05";
        const blob = new Blob([csvContent], { type: 'text/csv' });
        const sampleFile = new File([blob], 'sample_sales_data.csv', { type: 'text/csv' });
        setFile(sampleFile);
        setPrompt("Phân tích lợi nhuận của các sản phẩm và nhóm theo dòng sản phẩm tiềm năng nhất.");
    };

    return (
        <div className="card">
            <h2>1. Dữ liệu Phân Tích</h2>
            <p style={{ color: 'var(--text-secondary)', marginBottom: '1.5rem' }}>
                Tải lên file báo cáo (chỉ nhận .csv, .xlsx, .xls) và nhập yêu cầu phân tích cụ thể của bạn.
            </p>

            <div 
                className={`${styles.uploadBox} ${dragActive ? styles.dragActive : ''}`}
                onDragEnter={handleDrag}
                onDragOver={handleDrag}
                onDragLeave={handleDrag}
                onDrop={handleDrop}
                onClick={handleClick}
            >
                <input 
                    ref={fileInputRef}
                    type="file" 
                    className={styles.hiddenInput} 
                    accept=".csv, application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, application/vnd.ms-excel"
                    onChange={handleChange} 
                />
                
                {file ? (
                    <div>
                        <FileSpreadsheet size={48} className={styles.icon} />
                        <h3>{file.name}</h3>
                        <p className={styles.fileInfo}>{(file.size / (1024 * 1024)).toFixed(2)} MB</p>
                    </div>
                ) : (
                    <div>
                        <UploadCloud size={48} className={styles.icon} />
                        <h3>Kéo thả file vào đây hoặc Nhấn để chọn</h3>
                        <p style={{ marginTop: '0.5rem', color: 'var(--text-secondary)' }}>Tối đa 10MB</p>
                    </div>
                )}
            </div>
            
            {error && <p className={styles.error}>{error}</p>}

            <div className={styles.promptArea}>
                <label className={styles.promptLabel}>Prompt Phân Tích (Yêu cầu dành cho AI)</label>
                <textarea 
                    className={styles.promptInput}
                    value={prompt}
                    onChange={(e) => setPrompt(e.target.value)}
                    placeholder="VD: Hãy tìm ra sản phẩm bán chạy nhất..."
                ></textarea>
            </div>

            <div className={styles.actionRow}>
                <button 
                    className="button-primary" 
                    onClick={handleSubmit} 
                    disabled={!file || isUploading || !prompt.trim()}
                >
                    {isUploading ? 'Đang xử lý...' : 'Bắt đầu Phân Tích'}
                </button>
                <button 
                    className="button-secondary" 
                    onClick={loadSampleData}
                    disabled={isUploading}
                >
                    Dùng dữ liệu mẫu
                </button>
            </div>
        </div>
    );
};
