from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Dict, Any, Optional
import pandas as pd
import traceback
from app.executor import execute_code
from app.pdf_builder import build_pdf
import uvicorn

app = FastAPI(title="Chart Service", version="1.0.0")

class ExecuteRequest(BaseModel):
    code: str
    data: List[List[Any]]
    headers: List[str]
    job_id: str

class ChartResponse(BaseModel):
    name: str
    base64: str
    mime_type: str

class ExecuteResponse(BaseModel):
    success: bool
    charts: List[ChartResponse] = []
    stats: Dict[str, Any] = {}
    error: Optional[str] = None

class BuildPdfRequest(BaseModel):
    job_id: str
    charts: List[ChartResponse]
    insights: str
    file_name: str
    stats: Dict[str, Any]

class BuildPdfResponse(BaseModel):
    success: bool
    pdf_url: Optional[str] = None
    error: Optional[str] = None

@app.get("/health")
def health_check():
    return {"status": "ok"}

@app.post("/execute", response_model=ExecuteResponse)
def run_script(request: ExecuteRequest):
    try:
        # Convert list of lists to Pandas DataFrame
        if not request.headers or not request.data:
            raise ValueError("Headers and data cannot be empty")
        
        df = pd.DataFrame(request.data, columns=request.headers)
        
        # Gather basic stats
        stats = {
            "rowCount": df.shape[0],
            "colCount": df.shape[1],
            "summary": "Basic statistics gathered."
        }
        
        # Execute code in safe environment
        charts_data = execute_code(request.code, df)
        
        return ExecuteResponse(
            success=True,
            charts=charts_data,
            stats=stats
        )

    except Exception as e:
        error_trace = traceback.format_exc()
        print(error_trace) # Logging
        return ExecuteResponse(
            success=False,
            error=str(e),
            stats={ "rowCount": len(request.data) if hasattr(request, 'data') and request.data else 0 }
        )

@app.post("/build-pdf", response_model=BuildPdfResponse)
def create_pdf(request: BuildPdfRequest):
    try:
        url = build_pdf(
            job_id=request.job_id,
            charts=[c.dict() for c in request.charts],
            insights=request.insights,
            file_name=request.file_name,
            stats=request.stats
        )
        return BuildPdfResponse(success=True, pdf_url=url)
    except Exception as e:
        error_trace = traceback.format_exc()
        print(error_trace)
        return BuildPdfResponse(success=False, error=str(e))

if __name__ == "__main__":
    uvicorn.run("app.main:app", host="0.0.0.0", port=8000, reload=True)
