using NWLottoSystem.Utils;
using Serilog;
using static NWLottoSystem.Utils.Enums;

namespace NWLottoSystem.Library.LottoContext
{
    public class LottoHistorian
    {
        private readonly Thread[] _threads;
        private readonly Queue<DateTime> _queue;
        private readonly object _lock = new object();
        private readonly ILogger _logger;
        private readonly DatabaseContext _dbContext;
        private DateTime _lastUpdateTimer;

        public LottoHistorian(int threadNumber, ILogger logger, DatabaseContext dbContext, DateTime lastUpdateTimer)
        {
            _logger = logger;
            _threads = new Thread[threadNumber];
            _queue = new Queue<DateTime>();
            _dbContext = dbContext;
            _lastUpdateTimer = lastUpdateTimer;
        }

        public void Init()
        {
            DateTime maxDate = DateTime.Parse(Config.configVaraibles.HistorianMax);
            if (_lastUpdateTimer < maxDate)
            {
                DateTime tempTime = _lastUpdateTimer.AddDays(-3);
                while (tempTime < maxDate)
                {
                    _queue.Enqueue(tempTime);
                    tempTime = tempTime.AddDays(1);
                }
                _lastUpdateTimer = maxDate;
            }
        }

        public void Run()
        {
            if (_queue.Count > 0)
            {
                for (int i = 0; i < _threads.Length; i++)
                {
                    _threads[i] = new Thread(SearchLottoResults);
                    _threads[i].Start();
                }
                foreach (var thread in _threads)
                {
                    thread.Join();
                }
            }
        }

        private void SearchLottoResults()
        {
            DateTime tempDateTime = DateTime.MinValue;
            LottoResult lottoResult = new LottoResult(_logger, _dbContext, null);
            while (true)
            {
                lock (_lock)
                {
                    if (_queue.Count > 0)
                    {
                        tempDateTime = _queue.Dequeue();
                    }
                    else
                    {
                        break;
                    }
                }
                foreach (LottoGames game in Enum.GetValues(typeof(LottoGames)))
                {
                    if (game != LottoGames.lotto_plus_2)
                    {
                        _logger.Information("[{0}] [{1}] [{2}]", Thread.GetCurrentProcessorId(), tempDateTime, game);
                        lottoResult.PopulateResultsFromHistorian(tempDateTime, game);
                    }
                }
            }
        }
    }
}
