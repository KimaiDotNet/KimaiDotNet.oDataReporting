using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using ODataTeamMembership = MarkZither.KimaiDotNet.Reporting.ODataService.Models.TeamMembership;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Microsoft.Extensions.Caching.Memory;
using MarkZither.KimaiDotNet;
using KimaiDotNet.Reporting.ODataService;
using System.IO.Compression;
using CsvHelper;
using System.Globalization;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Controllers
{
    [Route("api/[controller]")]
    public class ExportController : Controller
    {
        private const string TimesheetsPath = "exports\\timesheets.csv";
        private const string UsersPath = "exports\\user.csv";
        private const string TeamsPath = "exports\\teams.csv";
        private const string TeamMembershipsPath = "exports\\teammembershipss.csv";
        private const string ActivitiesPath = "exports\\activities.csv";
        private const string CustomersPath = "exports\\customers.csv";
        private const string ProjectsPath = "exports\\projects.csv";
        private readonly KimaiOptions _kimaiOptions;
        private readonly ILogger<ExportController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        public ExportController(IOptions<KimaiOptions> kimaiOptions, ILogger<ExportController> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }
        [HttpGet(Name = "ExportToCSVUsingGet")]
        public async Task<IActionResult> Index(CancellationToken token = default)
        {
            var httpClient = _httpClientFactory.CreateClient(Constants.HttpClients.Kimai);
            var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
            var client = new KimaiClient(adapter);
            var zipFileMemoryStream = new MemoryStream();

            using (var zip = new ZipArchive(zipFileMemoryStream, ZipArchiveMode.Create, true))
            {
                _logger.LogInformation(new EventId(1, "Starting Export"), "Starting Export");
                FileInfo file = await CreateActivitiesFile(client, token);
                // write zip archive entries
                zip.CreateEntryFromFile(file.FullName, Path.GetFileName(file.FullName), CompressionLevel.Optimal);
                file.Delete();

                FileInfo timesheetsFile = await CreateTimesheetsFile(client, token);
                // write zip archive entries
                zip.CreateEntryFromFile(timesheetsFile.FullName, Path.GetFileName(timesheetsFile.FullName), CompressionLevel.Optimal);
                timesheetsFile.Delete();

                FileInfo usersFile = await CreateUsersFile(client, token);
                // write zip archive entries
                zip.CreateEntryFromFile(usersFile.FullName, Path.GetFileName(usersFile.FullName), CompressionLevel.Optimal);
                usersFile.Delete();

                FileInfo teamsFile = await CreateTeamsFile(client, token);
                // write zip archive entries
                zip.CreateEntryFromFile(teamsFile.FullName, Path.GetFileName(teamsFile.FullName), CompressionLevel.Optimal);
                teamsFile.Delete();

                FileInfo teamMembershipsFile = await CreateTeamMembershipsFile(client, token);
                // write zip archive entries
                zip.CreateEntryFromFile(teamMembershipsFile.FullName, Path.GetFileName(teamMembershipsFile.FullName), CompressionLevel.Optimal);
                teamMembershipsFile.Delete();

                FileInfo projectsFile = await CreateProjectsFile(client, token);
                // write zip archive entries
                zip.CreateEntryFromFile(projectsFile.FullName, Path.GetFileName(projectsFile.FullName), CompressionLevel.Optimal);
                projectsFile.Delete();

                FileInfo customersFile = await CreateCustomersFile(client, token);
                // write zip archive entries
                zip.CreateEntryFromFile(customersFile.FullName, Path.GetFileName(customersFile.FullName), CompressionLevel.Optimal);
                customersFile.Delete();
            }
            zipFileMemoryStream.Seek(0, SeekOrigin.Begin);
            return File(zipFileMemoryStream, "application/octect-stream", "KimaiExport.zip", true);
        }

        private async Task<FileInfo> CreateCustomersFile(KimaiClient client, CancellationToken token)
        {
            var url = $"{_kimaiOptions.Url}Customers";
            if (!_cache.TryGetValue(url, out IList<CustomerCollection>? customers))
            {
                customers = await client.Api.Customers.GetAsync(cancellationToken: token) ?? [];
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                _cache.Set(url, customers, TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(CustomersPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(customers);
            }
            var file = new FileInfo(CustomersPath);
            return file;
        }

        private async Task<FileInfo> CreateProjectsFile(KimaiClient client, CancellationToken token)
        {
            var url = $"{_kimaiOptions.Url}Projects";
            if (!_cache.TryGetValue(url, out IList<ProjectCollection>? projects))
            {
                projects = await client.Api.Projects.GetAsync(cancellationToken: token) ?? [];
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                _cache.Set(url, projects, TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(ProjectsPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(projects);
            }
            var file = new FileInfo(ProjectsPath);
            return file;
        }

        private async Task<FileInfo> CreateTeamMembershipsFile(KimaiClient client, CancellationToken token)
        {
            var url = "TeamMembership";
            if (!_cache.TryGetValue(url, out IList<ODataTeamMembership>? teamMemberships))
            {
                var teams = await client.Api.Teams.GetAsync(cancellationToken: token) ?? [];
                int memId = 1;
                teamMemberships = new List<ODataTeamMembership>();
                foreach (var item in teams)
                {
                    var teamEntity = await client.Api.Teams[item?.Id?.ToString() ?? "0"].GetAsync(cancellationToken: token);
                    if (teamEntity == null) continue;
                    foreach (var member in teamEntity.Members ?? [])
                    {
                        teamMemberships.Add(new ODataTeamMembership() { Id = memId, TeamId = teamEntity.Id.GetValueOrDefault(), UserId = member.User?.Id.GetValueOrDefault() ?? 0 });
                        memId++;
                    }
                }
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                _cache.Set(url, teamMemberships, TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(TeamMembershipsPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(teamMemberships);
            }
            var file = new FileInfo(TeamMembershipsPath);
            return file;
        }

        private async Task<FileInfo> CreateTeamsFile(KimaiClient client, CancellationToken token)
        {
            var url = "Teams";
            if (!_cache.TryGetValue(url, out IList<TeamCollection>? teams))
            {
                teams = await client.Api.Teams.GetAsync(cancellationToken: token) ?? [];
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                _cache.Set(url, teams, TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(TeamsPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(teams);
            }
            var file = new FileInfo(TeamsPath);
            return file;
        }

        private async Task<FileInfo> CreateUsersFile(KimaiClient client, CancellationToken token)
        {
            var url = "Users";
            if (!_cache.TryGetValue(url, out IList<UserCollection>? users))
            {
                users = await client.Api.Users.GetAsync(cancellationToken: token) ?? [];
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                _cache.Set(url, users, TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(UsersPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(users);
            }
            var file = new FileInfo(UsersPath);
            return file;
        }

        private async Task<FileInfo> CreateTimesheetsFile(KimaiClient client, CancellationToken token)
        {
            var url = "Timesheets";
            if (!_cache.TryGetValue(url, out List<TimesheetCollection>? timesheets))
            {
                timesheets = new List<TimesheetCollection>();
                var users = await client.Api.Users.GetAsync(cancellationToken: token) ?? [];
                foreach (var user in users)
                {
                    var usersTimesheets = await client.Api.Timesheets.GetAsync(
                        requestConfiguration: q =>
                        {
                            q.QueryParameters.User = user.Id?.ToString();
                            q.QueryParameters.Size = "1000";
                            q.QueryParameters.OrderBy = "id";
                            q.QueryParameters.Order = "DESC";
                        },
                        cancellationToken: token) ?? [];
                    timesheets.AddRange(usersTimesheets);
                }
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                _cache.Set(url, timesheets, TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(TimesheetsPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(timesheets);
            }
            var file = new FileInfo(TimesheetsPath);
            return file;
        }

        private async Task<FileInfo> CreateActivitiesFile(KimaiClient client, CancellationToken token)
        {
            var url = "Activity";
            if (!_cache.TryGetValue(url, out IList<ActivityCollection>? activities))
            {
                activities = await client.Api.Activities.GetAsync(cancellationToken: token) ?? [];
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                _cache.Set(url, activities, TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter("exports\\activities.csv", false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(activities);
            }
            var file = new FileInfo("exports\\activities.csv");
            return file;
        }
    }
}
