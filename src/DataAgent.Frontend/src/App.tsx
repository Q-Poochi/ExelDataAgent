import { useState } from 'react';
import { UploadZone } from './components/UploadZone';
import { DataPreview } from './components/DataPreview';
import { ProgressTimeline } from './components/ProgressTimeline';
import { ResultActions } from './components/ResultActions';
import { apiService } from './services/apiService';
import { useAnalysisSignalR } from './hooks/useAnalysisSignalR';
import type { JobUpdate } from './hooks/useAnalysisSignalR';
import './index.css';

function App() {
  const [file, setFile] = useState<File | null>(null);
  const [prompt, setPrompt] = useState<string>('');
  const [jobId, setJobId] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const { jobUpdate, connectionState } = useAnalysisSignalR(jobId);

  const handleFileSelect = async (selectedFile: File, userPrompt: string) => {
    setFile(selectedFile);
    setPrompt(userPrompt);
    setUploadError(null);
    setIsUploading(true);

    try {
      // 1. Upload File
      const { fileUrl, jobId: initialJobId } = await apiService.uploadFile(selectedFile);
      setJobId(initialJobId);

      // 2. Start Analysis
      await apiService.startAnalysis({
        fileUrl,
        prompt: userPrompt
      });

    } catch (error: any) {
      setUploadError(error.message || 'Có lỗi xảy ra khi bắt đầu phân tích.');
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <div className="container">
      <header style={{ marginBottom: '3rem', textAlign: 'center' }}>
        <h1 style={{ color: 'var(--primary-blue)', fontSize: '2.5rem' }}>DataAgent Platform</h1>
        <p style={{ color: 'var(--text-secondary)' }}>Automated Analysis & Reporting Powered by AI</p>
        {connectionState && connectionState !== 'Connected' && (
           <div style={{ color: 'orange', marginTop: '1rem' }}>
              SignalR Status: {connectionState}
           </div>
        )}
      </header>

      <main>
        <UploadZone onFileSelect={handleFileSelect} isUploading={isUploading} />
        
        {uploadError && (
          <div className="card" style={{ backgroundColor: '#ffebee', color: 'var(--error-color)', borderColor: 'var(--error-color)' }}>
              {uploadError}
          </div>
        )}

        {file && !uploadError && <DataPreview file={file} />}
        
        {jobId && <ProgressTimeline jobUpdate={jobUpdate} />}
        
        {jobUpdate?.status === 2 && <ResultActions jobUpdate={jobUpdate} />}
      </main>
    </div>
  );
}

export default App;
