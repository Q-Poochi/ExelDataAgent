import os
import io
import base64
from datetime import datetime
from jinja2 import Environment, FileSystemLoader
from weasyprint import HTML, CSS
import boto3
from botocore.exceptions import NoCredentialsError

MINIO_ENDPOINT = os.getenv("MINIO_ENDPOINT", "http://host.docker.internal:9000")
MINIO_ACCESS_KEY = os.getenv("MINIO_ACCESS_KEY", "minioadmin")
MINIO_SECRET_KEY = os.getenv("MINIO_SECRET_KEY", "minioadmin")
MINIO_BUCKET = os.getenv("MINIO_BUCKET", "data-agent-reports")

def generate_presigned_url(object_name):
    s3_client = boto3.client('s3',
                             endpoint_url=MINIO_ENDPOINT,
                             aws_access_key_id=MINIO_ACCESS_KEY,
                             aws_secret_access_key=MINIO_SECRET_KEY)
    try:
        url = s3_client.generate_presigned_url('get_object',
                                              Params={'Bucket': MINIO_BUCKET,
                                                      'Key': object_name},
                                              ExpiresIn=1800) # 30 mins
        return url
    except Exception as e:
        print(f"Error generating presigned URL: {e}")
        return None

def upload_to_minio(file_buffer, object_name):
    s3_client = boto3.client('s3',
                             endpoint_url=MINIO_ENDPOINT,
                             aws_access_key_id=MINIO_ACCESS_KEY,
                             aws_secret_access_key=MINIO_SECRET_KEY)
    try:
        # Check if bucket exists, if not create it
        try:
            s3_client.head_bucket(Bucket=MINIO_BUCKET)
        except:
            s3_client.create_bucket(Bucket=MINIO_BUCKET)

        s3_client.upload_fileobj(file_buffer, MINIO_BUCKET, object_name)
        return generate_presigned_url(object_name)
    except Exception as e:
        print(f"Error uploading to MinIO: {e}")
        return None

def build_pdf(job_id: str, charts: list, insights: str, file_name: str, stats: dict) -> str:
    # 1. Setup Jinja2 Environment
    env = Environment(loader=FileSystemLoader("templates"))
    template = env.get_template("report.html.j2")

    # 2. Render HTML
    html_out = template.render(
        file_name=file_name,
        date_created=datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        stats=stats,
        charts=charts,
        insights=insights
    )

    # 3. Convert HTML to PDF using WeasyPrint
    pdf_buffer = io.BytesIO()
    HTML(string=html_out).write_pdf(pdf_buffer)
    pdf_buffer.seek(0)
    
    # 4. Upload to MinIO
    object_name = f"{job_id}/{file_name}_report.pdf"
    signed_url = upload_to_minio(pdf_buffer, object_name)
    
    if not signed_url:
        raise Exception("Failed to upload PDF to MinIO")
        
    return signed_url
