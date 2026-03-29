using System;
using System.Threading.Tasks;
using DataAgent.Application.DTOs;

namespace DataAgent.Application.Interfaces;

public interface IAnalysisNotificationService
{
    Task NotifyJobUpdate(string jobId, JobUpdateDto dto);
    Task NotifyProgress(string jobId, int percent, string message);
}
