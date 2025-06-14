﻿using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volunteer_website.Models;

namespace Volunteer_website.Areas.Organization.Controllers
{
    [Area("Organization")]
    [Route("Organization/[controller]/[action]")]
    [Authorize("Org")]
    public class StatisticsController : Controller
    {
        private readonly VolunteerManagementContext _db;

        public StatisticsController(VolunteerManagementContext db)
        {
            _db = db;
        }
        [HttpGet]
        [HttpGet]
        public IActionResult Index(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            var orgId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                return RedirectToAction("Login", "Account");
            }

            var eventIds = _db.Events
                .Where(e => e.OrgId == orgId)
                .Select(e => e.EventId)
                .ToList();

            ViewBag.TotalVolunteers = _db.Registrations
                .Where(r => eventIds.Contains(r.EventId))
                .Select(r => r.VolunteerId)
                .Distinct()
                .Count();

            ViewBag.TotalEvents = _db.Events
                .Count(e => e.OrgId == orgId);

            ViewBag.TotalRegistrations = _db.Registrations
                .Count(r => eventIds.Contains(r.EventId));

            ViewBag.UncompletedEvaluations = _db.Evaluations
                .Count(e => !e.IsCompleted && eventIds.Contains(e.Reg.EventId));

            // Xử lý ngày mặc định nếu startDate hoặc endDate là null
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(startDateTime);
                endDateOnly = DateOnly.FromDateTime(endDateTime);
            }

            // Định dạng ngày thành chuỗi "yyyy-MM-dd" để hiển thị trong input
            ViewBag.StartDate = startDateOnly.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDateOnly.ToString("yyyy-MM-dd");

            // Lấy dữ liệu thống kê đăng ký theo thời gian
            var statisticsData = StatisticsRegistrationByTime(searchValue, startDate, endDate);
            var registrationChartData = statisticsData.Select(x => new
            {
                Label = $"{x.Year}-{x.Month:D2}-{x.Day:D2}",
                Count = x.Count
            }).ToList();

            ViewBag.RegistrationStatistics = new
            {
                Labels = registrationChartData.Select(x => x.Label).ToArray(),
                Data = registrationChartData.Select(x => x.Count).ToArray()
            };

            // Lấy dữ liệu thống kê donation theo thời gian
            var donationStatistics = StatisticsDonationByTime(searchValue, startDate, endDate);
            var donationChartData = donationStatistics.Select(x => new
            {
                Label = $"{x.Year}-{x.Month:D2}-{x.Day:D2}",
                TotalAmount = x.TotalAmount
            }).ToList();

            ViewBag.DonationStatistics = new
            {
                Labels = donationChartData.Select(x => x.Label).ToArray(),
                Data = donationChartData.Select(x => x.TotalAmount).ToArray()
            };

            // Lấy dữ liệu thống kê đăng ký theo sự kiện
            var registrationByEventData = StatisticsRegistrationByEvent(searchValue, startDate, endDate);
            var registrationByEventChartData = registrationByEventData.Select(x => new
            {
                Label = x.EventName,
                Count = x.Count
            }).ToList();

            ViewBag.RegistrationByEventStatistics = new
            {
                Labels = registrationByEventChartData.Select(x => x.Label).ToArray(),
                Data = registrationByEventChartData.Select(x => x.Count).ToArray()
            };

            // Lấy dữ liệu thống kê donation theo sự kiện
            var donationByEventData = StatisticsDonationByEvent(searchValue, startDate, endDate);
            var donationByEventChartData = donationByEventData.Select(x => new
            {
                Label = x.EventName,
                TotalAmount = x.TotalAmount
            }).ToList();

            ViewBag.DonationByEventStatistics = new
            {
                Labels = donationByEventChartData.Select(x => x.Label).ToArray(),
                Data = donationByEventChartData.Select(x => x.TotalAmount).ToArray()
            };

            // Lấy dữ liệu thống kê tổng hợp (tên sự kiện, số lượt tham gia, số tiền ủng hộ)
            var eventSummaryData = StatisticsEventSummary(searchValue, startDate, endDate);
            ViewBag.EventSummary = eventSummaryData;

            ViewBag.SearchValue = searchValue;

            return View();
        }


        #region Thống kê số lượt đăng kí theo thời gian
        public List<dynamic> StatisticsRegistrationByTime(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(startDateTime);
                endDateOnly = DateOnly.FromDateTime(endDateTime);
            }

            var orgId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var eventIds = eventQuery.Select(e => e.EventId).ToList();
            Console.WriteLine($"Event IDs for Registrations: {string.Join(", ", eventIds)}");

            var data = _db.Registrations
                .Where(r => eventIds.Contains(r.EventId) && r.RegisterAt.HasValue)
                .AsEnumerable() // Chuyển sang client-side để xử lý DateOnly
                .Where(r =>
                {
                    // Fixing the CS1503 error by converting DateOnly to DateTime before using it  
                    var registerDate = r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue);
                    // Convert DateOnly to DateTime for comparison
                    var startDateTime = startDateOnly.ToDateTime(TimeOnly.MinValue);
                    var endDateTime = endDateOnly.ToDateTime(TimeOnly.MinValue);
                   

                    return registerDate >= startDateTime && registerDate <= endDateTime;
                })
                .GroupBy(r => new
                {
                    Year = r.RegisterAt!.Value.Year,
                    Month = r.RegisterAt.Value.Month,
                    Day = r.RegisterAt.Value.Day // Thêm Day vào GroupBy
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day, // Thêm Day vào kết quả
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ThenBy(x => x.Day)
                .ToList();

            Console.WriteLine($"Registration Statistics Data: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

            return data.Select(x => (dynamic)x).ToList();
        }
        #endregion


        #region Thống kê số tiền Donation theo thời gian
        public List<dynamic> StatisticsDonationByTime(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(startDateTime);
                endDateOnly = DateOnly.FromDateTime(endDateTime);
            }

            var orgId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var eventIds = eventQuery.Select(e => e.EventId).ToList();
            Console.WriteLine($"Event IDs for Donations: {string.Join(", ", eventIds)}");

            var data = _db.Donations
                .Where(r => eventIds.Contains(r.EventId) && r.DonationDate.HasValue)
                .AsEnumerable() // Chuyển sang client-side để xử lý DateOnly
                .Where(r =>
                {
                    // Fixing the CS1503 error by converting DateOnly to DateTime before using it
                    var donationDate = r.DonationDate!.Value.ToDateTime(TimeOnly.MinValue);
                  
                    // Fixing the CS0019 errors by converting DateOnly to DateTime before comparison
                    var startDateTime = startDateOnly.ToDateTime(TimeOnly.MinValue);
                    var endDateTime = endDateOnly.ToDateTime(TimeOnly.MinValue);
                    return donationDate >= startDateTime && donationDate <= endDateTime;
                })
                .GroupBy(r => new
                {
                    Year = r.DonationDate!.Value.Year,
                    Month = r.DonationDate.Value.Month,
                    Day = r.DonationDate.Value.Day // Thêm Day vào GroupBy
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day, // Thêm Day vào kết quả
                    TotalAmount = g.Sum(d => d.Amount ?? 0)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ThenBy(x => x.Day)
                .ToList();

            Console.WriteLine($"Donation Statistics Data: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

            return data.Select(x => (dynamic)x).ToList();
        }
        #endregion



        #region Số lượt đăng kí theo sự kiện
        public List<dynamic> StatisticsRegistrationByEvent(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(startDateTime);
                endDateOnly = DateOnly.FromDateTime(endDateTime);
            }

            var orgId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var eventIds = eventQuery.Select(e => e.EventId).ToList();
            Console.WriteLine($"Event IDs for Registrations: {string.Join(", ", eventIds)}");

            var data = _db.Registrations
                .Where(r => eventIds.Contains(r.EventId) && r.RegisterAt.HasValue)
                .AsEnumerable() // Chuyển sang client-side để xử lý DateOnly
                 .Where(r =>
                 {
                     // Fixing the CS1503 error by converting DateOnly to DateTime before using it  
                     var registerDate = r.RegisterAt!.Value.ToDateTime(TimeOnly.MinValue);
                     // Convert DateOnly to DateTime for comparison
                     var startDateTime = startDateOnly.ToDateTime(TimeOnly.MinValue);
                     var endDateTime = endDateOnly.ToDateTime(TimeOnly.MinValue);
                     return registerDate >= startDateTime && registerDate <= endDateTime;
                 })
                .GroupBy(r => r.EventId)
                .Join(_db.Events, 
                      regGroup => regGroup.Key,
                      evt => evt.EventId,     
                      (regGroup, evt) => new
                      {
                          EventName = evt.Name ?? "Sự kiện không tên", 
                          Count = regGroup.Count() 
                      })
                .OrderBy(x => x.EventName)
                .ToList();

            Console.WriteLine($"Registration Statistics By Event: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

            return data.Select(x => (dynamic)x).ToList();
        }
        #endregion

        #region Số tiền ủng hộ theo sự kiện
        public List<dynamic> StatisticsDonationByEvent(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(startDateTime);
                endDateOnly = DateOnly.FromDateTime(endDateTime);
            }

            var orgId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var eventIds = eventQuery.Select(e => e.EventId).ToList();
            Console.WriteLine($"Event IDs for Donations: {string.Join(", ", eventIds)}");

            var data = _db.Donations
                .Where(d => eventIds.Contains(d.EventId) && d.DonationDate.HasValue)
                .AsEnumerable() // Chuyển sang client-side để xử lý DateOnly
                .Where(d => d.DonationDate!.Value >= startDateOnly && d.DonationDate.Value <= endDateOnly)
                .GroupBy(d => d.EventId)
                .Join(_db.Events,
                      donateGroup => donateGroup.Key,
                      evt => evt.EventId,
                      (donateGroup, evt) => new
                      {
                          EventName = evt.Name ?? "Sự kiện không tên",
                          TotalAmount = donateGroup.Sum(d => d.Amount ?? 0) // Tính tổng số tiền ủng hộ
                      })
                .OrderBy(x => x.EventName)
                .ToList();

            Console.WriteLine($"Donation Statistics By Event: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

            return data.Select(x => (dynamic)x).ToList();
        }
        #endregion


        #region Bảng thống kê tổng quát
        public List<dynamic> StatisticsEventSummary(string searchValue = "", string? startDate = null, string? endDate = null)
        {
            DateOnly startDateOnly, endDateOnly;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                startDateOnly = DateOnly.Parse(startDate);
                endDateOnly = DateOnly.Parse(endDate);
            }
            else
            {
                var endDateTime = DateTime.Now;
                var startDateTime = endDateTime.AddDays(-30);
                startDateOnly = DateOnly.FromDateTime(startDateTime);
                endDateOnly = DateOnly.FromDateTime(endDateTime);
            }

            var orgId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("No orgId found, returning empty list.");
                return new List<dynamic>();
            }

            // Lấy danh sách sự kiện
            var eventQuery = _db.Events.Where(e => e.OrgId == orgId);
            if (!string.IsNullOrEmpty(searchValue))
            {
                eventQuery = eventQuery.Where(e => e.Name != null && e.Name.Contains(searchValue));
            }

            var events = eventQuery.ToList();

            // Fix for CS8714: Ensure that the key used in ToDictionary is not nullable.  
            var registrationCounts = _db.Registrations
               .Where(r => events.Select(e => e.EventId).Contains(r.EventId))
               .AsEnumerable()
               .Where(r => r.RegisterAt.HasValue && r.RegisterAt.Value >= startDateOnly && r.RegisterAt.Value <= endDateOnly)
               .GroupBy(r => r.EventId)
               .Select(g => new { EventId = g.Key ?? string.Empty, Count = g.Count() })
               .ToDictionary(g => g.EventId, g => g.Count);

            // Fix for CS8714: Ensure that the key used in ToDictionary is not nullable.  
            var donationAmounts = _db.Donations
               .Where(d => events.Select(e => e.EventId).Contains(d.EventId))
               .AsEnumerable()
               .Where(d => d.DonationDate.HasValue && d.DonationDate.Value >= startDateOnly && d.DonationDate.Value <= endDateOnly)
               .GroupBy(d => d.EventId)
               .Select(g => new { EventId = g.Key ?? string.Empty, TotalAmount = g.Sum(d => d.Amount ?? 0) })
               .ToDictionary(g => g.EventId, g => g.TotalAmount);

            // Kết hợp dữ liệu
            var data = events.Select(e => new
            {
                EventName = e.Name ?? "Sự kiện không tên",
                RegistrationCount = registrationCounts.ContainsKey(e.EventId) ? registrationCounts[e.EventId] : 0,
                DonationAmount = donationAmounts.ContainsKey(e.EventId) ? donationAmounts[e.EventId] : 0
            })
            .OrderBy(x => x.EventName)
            .ToList();

            Console.WriteLine($"Event Summary Statistics: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");

            return data.Select(x => (dynamic)x).ToList();
        }
        #endregion
    }
}
