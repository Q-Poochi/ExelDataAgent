using System;
using MediatR;
using DataAgent.Application.DTOs;

namespace DataAgent.Application.Features.Files.GetFilePreview;

public class GetFilePreviewQuery : IRequest<FilePreviewResponse>
{
    public Guid FileId { get; set; }
    
    public GetFilePreviewQuery(Guid fileId)
    {
        FileId = fileId;
    }
}
