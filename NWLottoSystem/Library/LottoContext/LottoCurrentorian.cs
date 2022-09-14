using NWLottoSystem.Models;
using NWLottoSystem.Utils;
using Serilog;
using static NWLottoSystem.Utils.Enums;

namespace NWLottoSystem.Library.LottoContext
{
    public class LottoCurrentorian
    {
        private readonly ILogger _logger;
        private DateTime _lastUpdateTimer;
        private readonly Queue<LottoAPISearchPattern> _queue;
        private readonly LottoResult _lottoResult;
        private readonly HttpClient _client;

        public LottoCurrentorian(ILogger logger, DatabaseContext dbContext, DateTime lastUpdateTimer, HttpClient client)
        {
            _logger = logger;
            _lastUpdateTimer = lastUpdateTimer;
            _queue = new Queue<LottoAPISearchPattern>();
            _client = client;
            _lottoResult = new LottoResult(_logger, dbContext, _client);
        }

        public void Init()
        {
            if (_lastUpdateTimer < DateTime.Today)
            {
                while(_lastUpdateTimer < DateTime.Today)
                {
                    if ((DateTime.Today - _lastUpdateTimer).TotalDays > 30)
                    {
                        EnqueueDates(_lastUpdateTimer, 30);
                        _lastUpdateTimer = _lastUpdateTimer.AddDays(30);
                    }
                    else
                    {
                        EnqueueDates(_lastUpdateTimer, (int)(DateTime.Today - _lastUpdateTimer).TotalDays);
                        _lastUpdateTimer = DateTime.Today;
                    }
                }
            }
        }

        private void EnqueueDates(DateTime startDate, int numdays)
        {
            DateTime dateToEnqueue = startDate;
            bool canEnqueue = true;
            foreach (LottoGames game in Enum.GetValues(typeof(LottoGames)))
            {
                canEnqueue = true;

                if (game == LottoGames.powerball || game == LottoGames.powerball_plus)
                {
                    if (dateToEnqueue < Config.configVaraibles.powerballBiggerNumbersStartDate)
                    {
                        canEnqueue = false;
                        _logger.Warning("[{0}] [{1}] [{2}]<[{3}]", "Lottocurrentorian.EnqueueDates", game, dateToEnqueue, Config.configVaraibles.powerballBiggerNumbersStartDate);
                    }
                    else if (dateToEnqueue >= Config.configVaraibles.powerballBiggerNumbersStartDate && dateToEnqueue < Config.configVaraibles.powerballBiggerNumbersStartDate.AddDays(numdays))
                    {
                        dateToEnqueue = Config.configVaraibles.powerballBiggerNumbersStartDate;
                        _logger.Information("[{0}] [{1}] [{2}]<[{3}]", "Lottocurrentorian.EnqueueDates", game, dateToEnqueue, Config.configVaraibles.powerballBiggerNumbersStartDate);
                    }
                    else
                    {
                        dateToEnqueue = startDate;
                    }
                }

                if (game == LottoGames.lotto || game == LottoGames.lotto_plus_1 || game == LottoGames.lotto_plus_2)
                {
                    if (dateToEnqueue < Config.configVaraibles.lottoBiggerNumbersStartDate)
                    {
                        canEnqueue = false;
                        _logger.Warning("[{0}] [{1}] [{2}]<[{3}]", "Lottocurrentorian.EnqueueDates", game, dateToEnqueue, Config.configVaraibles.lottoBiggerNumbersStartDate);
                    }
                    else if (dateToEnqueue >= Config.configVaraibles.lottoBiggerNumbersStartDate && dateToEnqueue < Config.configVaraibles.lottoBiggerNumbersStartDate.AddDays(numdays))
                    {
                        dateToEnqueue = Config.configVaraibles.lottoBiggerNumbersStartDate;
                        _logger.Information("[{0}] [{1}] [{2}]<[{3}]", "Lottocurrentorian.EnqueueDates", game, dateToEnqueue, Config.configVaraibles.lottoBiggerNumbersStartDate);
                    }
                    else
                    {
                        dateToEnqueue = startDate;
                    }
                }

                if (canEnqueue)
                {
                    _queue.Enqueue(new LottoAPISearchPattern
                    {
                        startDate = dateToEnqueue,
                        endDate = dateToEnqueue.AddDays(31),
                        lottoGame = game,
                        results = numdays
                    });
                }
            }
        }

        public void Run()
        {
            while(_queue.Count > 0)
            {
                _lottoResult.PopulateResultsFromCurrentorian(_queue.Dequeue());
            }
        }
    }
}
