import React, { useEffect, useState } from 'react';
import Papa from 'papaparse';
import * as XLSX from 'xlsx';
import styles from './DataPreview.module.css';

interface DataPreviewProps {
    file: File | null;
}

interface StatInfo {
    totalRows: number;
    totalCols: number;
    numericCols: number;
    nullCount: number;
}

export const DataPreview: React.FC<DataPreviewProps> = ({ file }) => {
    const [headers, setHeaders] = useState<string[]>([]);
    const [rows, setRows] = useState<any[][]>([]);
    const [stats, setStats] = useState<StatInfo>({ totalRows: 0, totalCols: 0, numericCols: 0, nullCount: 0 });

    useEffect(() => {
        if (!file) return;

        const processData = (data: any[][]) => {
            if (data.length === 0) return;
            const h = data[0].map(String);
            const r = data.slice(1);
            
            let numCols = 0;
            let nulls = 0;
            
            // Basic estimation on first valid row 
            if (r.length > 0) {
                h.forEach((_, colIndex) => {
                    let isNumeric = true;
                    // check up to 5 rows to guess type
                    for (let i = 0; i < Math.min(5, r.length); i++) {
                        const val = r[i][colIndex];
                        if (val === null || val === undefined || val === '') {
                            nulls++;
                            continue;
                        }
                        if (isNaN(Number(val))) {
                            isNumeric = false;
                        }
                    }
                    if (isNumeric) numCols++;
                });
            }

            setHeaders(h);
            setRows(r.slice(0, 50)); // top 50 rows
            setStats({
                totalRows: r.length,
                totalCols: h.length,
                numericCols: numCols,
                nullCount: nulls // just an estimation from top 5 rows, or loop all if small. For performance, we just show rough.
            });
        };

        if (file.name.endsWith('.csv')) {
            Papa.parse(file, {
                complete: (results) => {
                    processData(results.data as any[][]);
                },
                skipEmptyLines: true,
                preview: 51 // header + 50
            });
        } else {
            const reader = new FileReader();
            reader.onload = (e) => {
                const data = new Uint8Array(e.target?.result as ArrayBuffer);
                const workbook = XLSX.read(data, { type: 'array' });
                const firstSheet = workbook.Sheets[workbook.SheetNames[0]];
                // Read up to 50 rows
                const jsonData = XLSX.utils.sheet_to_json(firstSheet, { header: 1, blankrows: false, defval: null });
                processData(jsonData as any[][]);
            };
            reader.readAsArrayBuffer(file);
        }
    }, [file]);

    if (!file) return null;

    const renderCell = (val: any) => {
        if (val === null || val === undefined || val === '') {
            return <span className={styles.typeNull}>null</span>;
        }
        if (!isNaN(Number(val))) {
            return <span className={styles.typeNumber}>{val}</span>;
        }
        return <span className={styles.typeText}>{String(val)}</span>;
    };

    return (
        <div className={`card ${styles.previewContainer}`}>
            <h2>2. Data Preview</h2>
            <div className={styles.statRow}>
                <div className={styles.statChip}>📊 Dòng: {stats.totalRows > 50 ? '50+' : stats.totalRows}</div>
                <div className={styles.statChip}>📋 Cột: {stats.totalCols}</div>
                <div className={styles.statChip}>🔢 Cột Số: {stats.numericCols}</div>
            </div>

            <div className={styles.tableWrapper}>
                <table className={styles.table}>
                    <thead>
                        <tr>
                            {headers.map((h, i) => <th key={i}>{h}</th>)}
                        </tr>
                    </thead>
                    <tbody>
                        {rows.map((row, rIdx) => (
                            <tr key={rIdx}>
                                {headers.map((_, cIdx) => (
                                    <td key={cIdx}>{renderCell(row[cIdx])}</td>
                                ))}
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            
            {stats.totalRows > 50 && (
                <p style={{ marginTop: '1rem', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                    * Đang hiển thị 50 dòng đầu tiên.
                </p>
            )}
        </div>
    );
};
