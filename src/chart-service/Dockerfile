FROM python:3.12-slim

WORKDIR /app

# Install system dependencies for WeasyPrint and Healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends \
    libpango-1.0-0 \
    libpangoft2-1.0-0 \
    libcairo2 \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Install Python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Create non-root user
RUN useradd -m -s /bin/bash appuser && \
    mkdir -p /app/data && \
    chown -R appuser:appuser /app
USER appuser

# Copy application code
COPY . .

# Environment variables
ENV PYTHONUNBUFFERED=1
ENV PORT=8000

# Expose port
EXPOSE 8000

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8000/ || exit 1

# Start the application using uvicorn correctly
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
