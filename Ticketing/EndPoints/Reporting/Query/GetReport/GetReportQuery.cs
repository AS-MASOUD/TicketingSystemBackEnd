using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Domain.Enums;

namespace Ticketing.EndPoints.Reporting.Query.GetReport
{
    public class GetReportQuery : IRequest<FileContentResult>
    {
        public string? ProjectId { get; set; } = "";
        public string? Priority { get; set; } = "";
        public string? RequestType { get; set; } = "";
        public string? StatusId { get; set; } = "";
        public string? DeveloperId { get; set; } = "";
        public string? IsSchadule { get; set; } = "";
        public DateTime? InsertStartDateTime { get; set; }
        public DateTime? InsertEndDateTime { get; set; }
        public DateTime? CloseStartDateTime { get; set; }
        public DateTime? CloseEndDateTime { get; set; }
    }
}
