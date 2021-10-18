using MarkZither.KimaiDotNet.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Models;
using MarkZither.KimaiDotNet.Reporting.ODataService.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MonkeyCache.LiteDB;
using MarkZither.KimaiDotNet;
using System.IO.Compression;
using CsvHelper;
using System.Globalization;

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
        public ExportController(IOptions<KimaiOptions> kimaiOptions, ILogger<ExportController> logger)
        {
            _kimaiOptions = kimaiOptions.Value;
            _logger = logger;
        }
        [HttpGet(Name = "ExportToCSVUsingGet")]
        public IActionResult Index()
        {
            var Client = new HttpClient();
            Client.BaseAddress = new Uri(_kimaiOptions.Url);
            Client.DefaultRequestHeaders.Add("X-AUTH-USER", _kimaiOptions.Username);
            Client.DefaultRequestHeaders.Add("X-AUTH-TOKEN", _kimaiOptions.Password);
            Kimai2APIDocs docs = new Kimai2APIDocs(Client, disposeHttpClient: false);
            var zipFileMemoryStream = new MemoryStream();

            using (var zip = new ZipArchive(zipFileMemoryStream, ZipArchiveMode.Create, true))
            {
                _logger.LogInformation(new EventId(1, "Starting Export"), "Starting Export");
                FileInfo file = CreateActivitiesFile(docs);
                // write zip archive entries
                zip.CreateEntryFromFile(file.FullName, Path.GetFileName(file.FullName), CompressionLevel.Optimal);
                file.Delete();

                FileInfo timesheetsFile = CreateTimesheetsFile(docs);
                // write zip archive entries
                zip.CreateEntryFromFile(timesheetsFile.FullName, Path.GetFileName(timesheetsFile.FullName), CompressionLevel.Optimal);
                timesheetsFile.Delete();

                FileInfo usersFile = CreateUsersFile(docs);
                // write zip archive entries
                zip.CreateEntryFromFile(usersFile.FullName, Path.GetFileName(usersFile.FullName), CompressionLevel.Optimal);
                usersFile.Delete();

                FileInfo teamsFile = CreateTeamsFile(docs);
                // write zip archive entries
                zip.CreateEntryFromFile(teamsFile.FullName, Path.GetFileName(teamsFile.FullName), CompressionLevel.Optimal);
                teamsFile.Delete();

                FileInfo teamMembershipsFile = CreateTeamMembershipsFile(docs);
                // write zip archive entries
                zip.CreateEntryFromFile(teamMembershipsFile.FullName, Path.GetFileName(teamMembershipsFile.FullName), CompressionLevel.Optimal);
                teamMembershipsFile.Delete();

                FileInfo projectsFile = CreateProjectsFile(docs);
                // write zip archive entries
                zip.CreateEntryFromFile(projectsFile.FullName, Path.GetFileName(projectsFile.FullName), CompressionLevel.Optimal);
                projectsFile.Delete();

                FileInfo customersFile = CreateCustomersFile(docs);
                // write zip archive entries
                zip.CreateEntryFromFile(customersFile.FullName, Path.GetFileName(customersFile.FullName), CompressionLevel.Optimal);
                customersFile.Delete();
            }
            zipFileMemoryStream.Seek(0, SeekOrigin.Begin);
            return File(zipFileMemoryStream, "application/octect-stream", "KimaiExport.zip", true);
        }

        private FileInfo CreateCustomersFile(Kimai2APIDocs docs)
        {
            var url = $"{_kimaiOptions.Url}Customers";
            IList<CustomerCollection> customers = new List<CustomerCollection>();
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                customers = Barrel.Current.Get<List<CustomerCollection>>(key: url);
            }
            else
            {
                customers = docs.ListCustomersUsingGet();
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                Barrel.Current.Add(key: url, data: customers, expireIn: TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(CustomersPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(customers);
            }
            var file = new FileInfo(CustomersPath);
            return file;
        }

        private FileInfo CreateProjectsFile(Kimai2APIDocs docs)
        {
            var url = $"{_kimaiOptions.Url}Projects";
            IList<ProjectCollection> projects = new List<ProjectCollection>();
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                projects = Barrel.Current.Get<List<ProjectCollection>>(key: url);
            }
            else
            {
                projects = docs.ListProjectUsingGet();
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                Barrel.Current.Add(key: url, data: projects, expireIn: TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(ProjectsPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(projects);
            }
            var file = new FileInfo(ProjectsPath);
            return file;
        }

        private FileInfo CreateTeamMembershipsFile(Kimai2APIDocs docs)
        {
            var url = "TeamMembership";
            IList<TeamMembership> teamMemberships = new List<TeamMembership>();
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                teamMemberships = Barrel.Current.Get<List<TeamMembership>>(key: url);
            }
            else
            {
                var teams = docs.ListTeamUsingGet();
                int memId = 1;
                foreach (var item in teams)
                {
                    var teamEntity = docs.GetTeamByIdUsingGet(item?.Id?.ToString());
                    foreach (var user in teamEntity.Users)
                    {
                        teamMemberships.Add(new TeamMembership() { Id = memId, TeamId = teamEntity.Id.GetValueOrDefault(), UserId = user.Id.GetValueOrDefault() });
                        memId++;
                    }
                }
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                Barrel.Current.Add(key: url, data: teamMemberships, expireIn: TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(TeamMembershipsPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(teamMemberships);
            }
            var file = new FileInfo(TeamMembershipsPath);
            return file;
        }

        private FileInfo CreateTeamsFile(Kimai2APIDocs docs)
        {
            var url = "Teams";
            IList<TeamCollection> teams = new List<TeamCollection>();
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                teams = Barrel.Current.Get<List<TeamCollection>>(key: url);
            }
            else
            {
                teams = docs.ListTeamUsingGet();
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                Barrel.Current.Add(key: url, data: teams, expireIn: TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(TeamsPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(teams);
            }
            var file = new FileInfo(TeamsPath);
            return file;
        }

        private FileInfo CreateUsersFile(Kimai2APIDocs docs)
        {
            var url = "Users";
            IList<UserCollection> users = new List<UserCollection>();
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                users = Barrel.Current.Get<List<UserCollection>>(key: url);
            }
            else
            {
                users = docs.ListUsersUsingGet();
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                Barrel.Current.Add(key: url, data: users, expireIn: TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(UsersPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(users);
            }
            var file = new FileInfo(UsersPath);
            return file;
        }

        private FileInfo CreateTimesheetsFile(Kimai2APIDocs docs)
        {
            var url = "Timesheets";
            IList<TimesheetCollection> timesheets = new List<TimesheetCollection>();
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                timesheets = Barrel.Current.Get<List<TimesheetCollection>>(key: url);
            }
            else
            {
                timesheets = docs.ListTimesheetsRecordsUsingGet();
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                Barrel.Current.Add(key: url, data: timesheets, expireIn: TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter(TimesheetsPath, false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(timesheets);
            }
            var file = new FileInfo(TimesheetsPath);
            return file;
        }

        private static FileInfo CreateActivitiesFile(Kimai2APIDocs docs)
        {
            var url = "Activity";
            IList<ActivityCollection> activities = new List<ActivityCollection>();
            //Dev handles checking if cache is expired
            if (!Barrel.Current.IsExpired(key: url))
            {
                activities = Barrel.Current.Get<List<ActivityCollection>>(key: url);
            }
            else
            {
                activities = docs.ListActivitiesUsingGet();
                //Saves the cache and pass it a timespan for expiration
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                double secs = untilMidnight.TotalSeconds;
                Barrel.Current.Add(key: url, data: activities, expireIn: TimeSpan.FromSeconds(secs));
            }

            using (var writer = new StreamWriter("exports\\activities.csv",false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(activities);
            }
            var file = new FileInfo("exports\\activities.csv");
            return file;
        }
    }
}
