using NWLottoSystem.Library;
using NWLottoSystem.Library.LottoContext;
using NWLottoSystem.Utils;
using Serilog;

namespace NWLottoSystem
{
    internal class Program
    {
        private static ILogger _logger;
        private static DatabaseContext _dbContext;
        private static HttpClient _httpClient;

        static void Main(string[] args)
        {
            Init();
            Run();
        }

        private static void Init()
        {
            Config.LoadConfig();
            _logger = Factory.GetLogger();
            _dbContext = Factory.GetDBContext(_logger, Config.GetConnectionString());
            _httpClient = new HttpClient();
        }

        private static void Run()
        {
            DateTime lastEntryTime = _dbContext.GetLastResultDate();
            DateTime past = DateTime.Now;
            DateTime future = DateTime.MinValue;
            LottoStatistician lottoStatistician = new LottoStatistician(_dbContext, _logger);
            LottoCurrentorian lottoCurrentorian = new LottoCurrentorian(_logger, _dbContext, lastEntryTime, _httpClient);
            LottoGenerator lottoGenerator = new LottoGenerator(_logger, lottoStatistician);
            TelegramAPI telegramAPI = new TelegramAPI(_logger, _httpClient, _dbContext, lottoGenerator, lottoStatistician);
            while (true)
            {
                past = DateTime.Now;
                if (past > future)
                {
                    lottoCurrentorian.Init();
                    lottoCurrentorian.Run();
                    lottoStatistician.Run();
                    telegramAPI.CheckEntries();
                    future = DateTime.Today.AddDays(1).AddHours(12);
                }
                telegramAPI.ScanForMessages();
                telegramAPI.ActionResponses();
                _logger.Information("[{0}] [{1}]ms [{2}]", "Program.Run", "Execution complete, Waiting", Config.configVaraibles.sleepTimer);
                Thread.Sleep(Config.configVaraibles.sleepTimer);
            }
        }
    }
}