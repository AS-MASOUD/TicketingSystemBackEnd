using MediatR;
using Ticketing.Services;
using System.Globalization;
using System.Text.Json;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Enums;

namespace Ticketing.EndPoints.Dashboard.Query.GetDashboardChartData
{
    public partial class GetDashboardChartDataHandler
    {
        public class Handler(ITicketService ticketService, ILogger<GetDashboardChartDataHandler> _logger, ICurrentUserService currentUser) : IRequestHandler<GetDashboardChartDataQuery, object>
        {
            public async Task<object> Handle(GetDashboardChartDataQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    var visibleTickets = await ticketService.ListAsync(TicketVisibility.For(currentUser));
                    #region Develop in RFB
                    var developInRFPTicket = visibleTickets.Where(a => a.RequestTypeId == RequestType.Develop &&
                                                                           a.IsSchedule == IsSchedule.yes &&
                                                                          (a.StatusId == (int)StatusId.inLine ||
                                                                           a.StatusId == (int)StatusId.inProgress ||
                                                                           a.StatusId == (int)StatusId.awaitingConfirmation ||
                                                                           a.StatusId == (int)StatusId.done)).ToList();

                    var developInRFPDoneTicket = visibleTickets.Where(a => a.RequestTypeId == RequestType.Develop && a.ProcessEndDateTime!=null &&
                                                                               a.IsSchedule == IsSchedule.yes &&
                                                                              (a.StatusId == (int)StatusId.awaitingConfirmation ||
                                                                               a.StatusId == (int)StatusId.done)).ToList();
                    #endregion

                    #region Develop out RFB
                    var developOutRFPTicket = visibleTickets.Where(a => a.RequestTypeId == RequestType.Develop &&
                                                                            a.IsSchedule == IsSchedule.no &
                                                                           (a.StatusId == (int)StatusId.inLine ||
                                                                            a.StatusId == (int)StatusId.inProgress ||
                                                                            a.StatusId == (int)StatusId.awaitingConfirmation ||
                                                                            a.StatusId == (int)StatusId.done)).ToList();

                    var developOutRFPDoneTickets = visibleTickets.Where(a => a.RequestTypeId == RequestType.Develop && a.ProcessEndDateTime != null &&
                                                                                 a.IsSchedule == IsSchedule.no &&
                                                                                (a.StatusId == (int)StatusId.awaitingConfirmation ||
                                                                                 a.StatusId == (int)StatusId.done)).ToList();
                    #endregion

                    #region Support Tickets
                    // TODO: Change it to dynamic value 
                    int CompanyCommitmentToSupportTime = 1160;
                    var SupportTicket = visibleTickets.Where(a => a.RequestTypeId == RequestType.Support &&
                                                                      a.ProcessEndDateTime != null).ToList();
                    #endregion

                    //~~~~~~~~~~~~~~~~~~~~~~~~Return~~~~~~~~~~~~~~~~~~~~~~~~//

                    #region Return Variables

                    var developResultYearInRFP = new List<DevelopResultYear>();
                    var developResultYearOutRFP = new List<DevelopResultYear>();
                    var supportResultYear = new List<SupportResultYear>();
                    var developerResultYear = new List<DeveloperResultYear>();
                    var p = new PersianCalendar();

                    #endregion

                    #region Developed result
                    for (var i = 1; i < 13; i++)
                    {
                        developResultYearInRFP.Add(new DevelopResultYear
                        {
                            Month = GetMonth(i),
                            Total = developInRFPTicket.Count(a => p.GetMonth((DateTime)a.LastChangeDatetime) == i),
                            Done = developInRFPDoneTicket.Count(a => p.GetMonth((DateTime)a.ProcessEndDateTime) == i)
                        });

                        developResultYearOutRFP.Add(new DevelopResultYear
                        {
                            Month = GetMonth(i),
                            Total = developOutRFPTicket.Count(a => p.GetMonth((DateTime)a.LastChangeDatetime) == i),
                            Done = developOutRFPDoneTickets.Count(a => p.GetMonth((DateTime)a.ProcessEndDateTime) == i)
                        });
                    }
                    #endregion

                    #region support year result
                    for (var i = 1; i < 13; i++)
                    {
                        var tickets = SupportTicket.Where(a => p.GetMonth((DateTime)a.ProcessEndDateTime) == i);
                        var totalTime = 0;
                        foreach (var ticket in tickets)
                        {
                            if (int.TryParse(ticket.TicketTime, out var time))
                            {
                                totalTime += time;
                            }
                        }
                        supportResultYear.Add(new SupportResultYear
                        {
                            Month = GetMonth(i),
                            CompanyCommitmentTime = CompanyCommitmentToSupportTime,
                            SupportTime = totalTime,
                        });
                    }
                    #endregion

                    #region Developer month result

                    #region List of developers
                    var developers = new Dictionary<string, Developer>
                    {
                         { "p_rezayeh", Developer.p_rezayeh },
                         { "m_bagheri", Developer.m_bagheri },
                         { "t_hagigi", Developer.t_hagigi },
                         { "m_borji", Developer.m_borji },
                         { "m_salehi", Developer.m_salehi },
                         { "Sh_kazempour", Developer.Sh_kazempour },
                         { "e_darvishi", Developer.e_darvishi },
                         { "s_mohamadzadeh", Developer.s_mohamadzadeh }
                    };
                    #endregion

                    #region Initialize result lists
                    var monthDataCount_support = new List<DeveloperData>();
                    var monthDataTime_support = new List<DeveloperData>();
                    var monthDataCount_develop = new List<DeveloperData>();
                    var monthDataTime_develop = new List<DeveloperData>();
                    #endregion

                    #region Fetch tickets and process data
                    foreach (var developer in developers)
                    {
                        #region Find ticket
                        var tickets = visibleTickets.Where(a =>
                            a.DeveloperId == developer.Value &&
                            (a.StatusId == (int)StatusId.awaitingConfirmation || a.StatusId == (int)StatusId.done)).ToList();

                        var supportTickets = tickets.Where(a =>
                            a.ProcessEndDateTime.HasValue && p.GetMonth(a.ProcessEndDateTime.Value) == request.monthId &&
                            a.RequestTypeId == RequestType.Support);

                        var developTickets = tickets.Where(a =>
                            a.ProcessEndDateTime.HasValue && p.GetMonth(a.ProcessEndDateTime.Value) == request.monthId &&
                            a.RequestTypeId == RequestType.Develop);
                        #endregion

                        #region Support count
                        monthDataCount_support.Add(new DeveloperData
                        {
                            name = developer.Key,
                            value = supportTickets.Count()
                        });
                        #endregion

                        #region Support time
                        monthDataTime_support.Add(new DeveloperData
                        {
                            name = developer.Key,
                            value = supportTickets.Sum(ticket => int.TryParse(ticket.TicketTime, out var time) && time > 0 ? time : 0)
                        });
                        #endregion

                        #region Develop count
                        monthDataCount_develop.Add(new DeveloperData
                        {
                            name = developer.Key,
                            value = developTickets.Count()
                        });
                        #endregion

                        #region Develop time
                        monthDataTime_develop.Add(new DeveloperData
                        {
                            name = developer.Key,
                            value = developTickets.Sum(ticket => int.TryParse(ticket.TicketTime, out var time) && time > 0 ? time : 0)
                        });
                        #endregion
                    }
                    #endregion

                    #endregion

                    #region result
                    var result = new
                    {
                        DevelopResultYearInRFP = developResultYearInRFP,
                        DevelopResultYearOutRFP = developResultYearOutRFP,
                        SupportResultYear = supportResultYear,
                        DeveloperResultCount_support = monthDataCount_support,
                        DeveloperResultTime_support = monthDataTime_support,
                        DeveloperResultCount_develop = monthDataCount_develop,
                        DeveloperResultTime_develop = monthDataTime_develop
                    };
                    #endregion

                    return JsonSerializer.Serialize(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Get Error : GetTicketMassageList- Handle " + ex.Message);
                }

                return null;
            }

            #region GetMonth method
            public string GetMonth(int month)
            {
                if (month == 1) return "فروردین";
                else if (month == 2) return "اردیبهشت";
                else if (month == 3) return "خرداد";
                else if (month == 4) return "تیر";
                else if (month == 5) return "مرداد";
                else if (month == 6) return "شهریور";
                else if (month == 7) return "مهر";
                else if (month == 8) return "ابان";
                else if (month == 9) return "اذر";
                else if (month == 10) return "دی";
                else if (month == 11) return "بهمن";
                else if (month == 12) return "اسفند";
                return "";
            }

            #endregion
        }
    }
}
