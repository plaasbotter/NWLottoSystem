using Newtonsoft.Json;
using NWLottoSystem.Models;
using Serilog;
using System.Text;
using static NWLottoSystem.Utils.Enums;

namespace NWLottoSystem.Library.LottoContext
{
    public class LottoStatistician
    {
        private readonly ILogger _logger;
        private readonly DatabaseContext _dbContext;
        private readonly PowerBallStats powerBallStats;
        private readonly LottoStats lottoStats;
        private readonly DailyLottoStats dailyLottoStats;
        private DateTime _lastUpdateTime;

        public LottoStatistician(DatabaseContext dbContext, ILogger logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            powerBallStats = new PowerBallStats();
            lottoStats = new LottoStats();
            dailyLottoStats = new DailyLottoStats();
            _lastUpdateTime = DateTime.Today.AddDays(-1);
        }

        public void Run()
        {
            if (_lastUpdateTime < DateTime.Today)
            {
                try
                {
                    UpdatePowerBall();
                    UpdateLotto();
                    UpdateDailyLotto();
                    using (FileStream fs = new FileStream($"{_lastUpdateTime.ToString("yyyy-MM-dd")}_pb_stats.json", FileMode.Create))
                    {
                        fs.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(powerBallStats)));
                    }
                    using (FileStream fs = new FileStream($"{_lastUpdateTime.ToString("yyyy-MM-dd")}_lt_stats.json", FileMode.Create))
                    {
                        fs.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(lottoStats)));
                    }
                    using (FileStream fs = new FileStream($"{_lastUpdateTime.ToString("yyyy-MM-dd")}_dl_stats.json", FileMode.Create))
                    {
                        fs.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dailyLottoStats)));
                    }
                    _lastUpdateTime = DateTime.Today;
                }
                catch (Exception err)
                {
                    _logger.Error(err, "[{0}]", "LottoStaticstician.Run()");
                }
            }
        }

        private void UpdateLotto()
        {
            _logger.Information("[{0}]", "LottoStatictician.UpdateLotto");
            _dbContext.UpdateCount(ref lottoStats.count, lottoStats.lottoTypeWhere);
            _dbContext.UpdateOdds(lottoStats.odds, 6, true, lottoStats.lottoTypeWhere);
            _dbContext.UpdateHighs(lottoStats.highs, lottoStats.highValue, 6, true, lottoStats.lottoTypeWhere);
            _dbContext.UpdateBallFrequency(lottoStats.ballFrequency, 6, true, lottoStats.lottoTypeWhere);
            _dbContext.UpdateSums(lottoStats.sums, 6, true, lottoStats.lottoTypeWhere);
            _dbContext.GetLastResult(lottoStats.lastResult, 6, true, lottoStats.lottoTypeWhere, lottoStats.games.Count);
            UpdateFrequencyAverages(lottoStats);
        }

        private void UpdateFrequencyAverages(StatsBaseClass lottoStats, LottoGames lottoGame = LottoGames.unknown)
        {
            foreach (var ball in lottoStats.ballFrequency)
            {
                lottoStats.ballFrequencyAverage += ball;
            }
            lottoStats.ballFrequencyAverage /= (double)lottoStats.ballFrequency.Length;
            if (lottoGame == LottoGames.powerball)
            {
                foreach (var ball in powerBallStats.powerballFrequency)
                {
                    powerBallStats.powerballFrequencyAverage += ball;
                }
                powerBallStats.powerballFrequencyAverage /= (double)powerBallStats.powerballFrequency.Length;
            }
        }

        private void UpdatePowerBall()
        {
            _logger.Information("[{0}]", "LottoStatictician.UpdatePowerball");
            _dbContext.UpdateCount(ref powerBallStats.count, powerBallStats.lottoTypeWhere);
            _dbContext.UpdateOdds(powerBallStats.odds, 5, false, powerBallStats.lottoTypeWhere);
            _dbContext.UpdateHighs(powerBallStats.highs, powerBallStats.highValue, 5, false, powerBallStats.lottoTypeWhere);
            _dbContext.UpdateBallFrequency(powerBallStats.ballFrequency, 5, false, powerBallStats.lottoTypeWhere);
            _dbContext.UpdateSums(powerBallStats.sums, 5, false, powerBallStats.lottoTypeWhere);
            _dbContext.UpdatePowerBallFrequency(powerBallStats.powerballFrequency, powerBallStats.lottoTypeWhere);
            _dbContext.GetLastResult(powerBallStats.lastResult, 5, true, powerBallStats.lottoTypeWhere, powerBallStats.games.Count);
            UpdateFrequencyAverages(powerBallStats, LottoGames.powerball);
        }

        private void UpdateDailyLotto()
        {
            _logger.Information("[{0}]", "LottoStatictician.UpdateDailyLotto");
            _dbContext.UpdateCount(ref dailyLottoStats.count, dailyLottoStats.lottoTypeWhere);
            _dbContext.UpdateOdds(dailyLottoStats.odds, 5, false, dailyLottoStats.lottoTypeWhere);
            _dbContext.UpdateHighs(dailyLottoStats.highs, dailyLottoStats.highValue, 5, false, dailyLottoStats.lottoTypeWhere);
            _dbContext.UpdateBallFrequency(dailyLottoStats.ballFrequency, 5, false, dailyLottoStats.lottoTypeWhere);
            _dbContext.UpdateSums(dailyLottoStats.sums, 5, false, dailyLottoStats.lottoTypeWhere);
            _dbContext.GetLastResult(dailyLottoStats.lastResult, 5, false, dailyLottoStats.lottoTypeWhere, dailyLottoStats.games.Count);
            UpdateFrequencyAverages(dailyLottoStats);
        }

        internal string GetLastPowerballResult()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Powerball {string.Join(' ', powerBallStats.lastResult[LottoGames.powerball])}");
            sb.AppendLine($"Powerball_plus {string.Join(' ', powerBallStats.lastResult[LottoGames.powerball_plus])}");
            return sb.ToString();
        }

        internal string GetLastLottoResult()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Lotto {string.Join(' ', lottoStats.lastResult[LottoGames.lotto])}");
            sb.AppendLine($"Lotto_plus_1 {string.Join(' ', lottoStats.lastResult[LottoGames.lotto_plus_1])}");
            sb.AppendLine($"Lotto_plus_2 {string.Join(' ', lottoStats.lastResult[LottoGames.lotto_plus_2])}");
            return sb.ToString();
        }

        internal string GetLastDailyResult()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Daily {string.Join(' ', dailyLottoStats.lastResult[LottoGames.daily_lotto])}");
            return sb.ToString();
        }

        public double TestHighProbability(int[] balls, int powerball, LottoGames game)
        {
            double returnValue = 0;
            switch (game)
            {
                case LottoGames.powerball:
                    returnValue = CalculateGeneralProbability(balls, powerBallStats);
                    returnValue = CalculateHighGeneralProbability(balls, powerball, returnValue, game, powerBallStats);
                    break;
                case LottoGames.lotto:
                    returnValue = CalculateGeneralProbability(balls, lottoStats);
                    returnValue = CalculateHighGeneralProbability(balls, 0, returnValue, game, lottoStats);
                    break;
                case LottoGames.daily_lotto:
                    returnValue = CalculateGeneralProbability(balls, dailyLottoStats);
                    returnValue = CalculateHighGeneralProbability(balls, 0, returnValue, game, dailyLottoStats);
                    break;
            }
            return returnValue;
        }

        private double CalculateHighGeneralProbability(int[] balls, int powerball, double returnValue, LottoGames game, StatsBaseClass baseStats)
        {
            for (int i = 0; i < balls.Length; i++)
            {
                returnValue *= (double)baseStats.ballFrequency[balls[i] - 1] / (double)baseStats.count;
            }
            if (game == LottoGames.powerball)
            {
                returnValue *= (double)baseStats.powerballFrequency[powerball - 1] / (double)baseStats.count;
            }
            return returnValue;
        }

        public double TestLowProbability(int[] balls, int powerball, LottoGames game)
        {
            double returnValue = 0;
            switch (game)
            {
                case LottoGames.powerball:
                    returnValue = CalculateGeneralProbability(balls, powerBallStats);
                    returnValue = CalculateLowGeneralProbability(balls, powerball, returnValue, game, powerBallStats);
                    break;
                case LottoGames.lotto:
                    returnValue = CalculateGeneralProbability(balls, lottoStats);
                    returnValue = CalculateLowGeneralProbability(balls, 0, returnValue, game, lottoStats);
                    break;
                case LottoGames.daily_lotto:
                    returnValue = CalculateGeneralProbability(balls, dailyLottoStats);
                    returnValue = CalculateLowGeneralProbability(balls, 0, returnValue, game, dailyLottoStats);
                    break;
            }
            return returnValue;
        }

        private double CalculateGeneralProbability(int[] balls, StatsBaseClass baseStats)
        {
            double statVal = 0;
            double returnValue = 1000000;
            int highs = 0;
            int odds = 0;
            int sum = 0;
            foreach (var ball in balls)
            {
                highs += (int)(ball / baseStats.highValue);
                odds += ball % 2;
                sum += ball;
            }
            statVal = (double)baseStats.sums[sum - 1] / (double)baseStats.count;
            returnValue *= statVal;
            statVal *= (double)baseStats.highs[highs] / (double)baseStats.count;
            returnValue *= statVal;
            statVal *= (double)baseStats.odds[odds] / (double)baseStats.count;
            returnValue *= statVal;
            return returnValue;
        }

        private double CalculateLowGeneralProbability(int[] balls, int powerball, double returnValue, LottoGames game, StatsBaseClass baseStats)
        {
            double statVal = 0;
            for (int i = 0; i < balls.Length; i++)
            {
                statVal = 1.0 - ((double)baseStats.ballFrequency[balls[i] - 1] / (double)baseStats.count);
                returnValue *= statVal;
            }
            if (game == LottoGames.powerball)
            {
                statVal = 1.0 - ((double)baseStats.powerballFrequency[powerball - 1] / (double)baseStats.count);
                returnValue *= statVal;
            }
            return returnValue;
        }

        public double TestDistanceProbability(int[] balls, int powerball, LottoGames game)
        {
            double returnValue = 0;
            switch (game)
            {
                case LottoGames.powerball:
                    returnValue = CalculateGeneralProbability(balls, powerBallStats);
                    returnValue = CalculateDistanceGeneralProbability(balls, powerball, returnValue, game, powerBallStats);
                    break;
                case LottoGames.lotto:
                    returnValue = CalculateGeneralProbability(balls, lottoStats);
                    returnValue = CalculateDistanceGeneralProbability(balls, 0, returnValue, game, lottoStats);
                    break;
                case LottoGames.daily_lotto:
                    returnValue = CalculateGeneralProbability(balls, dailyLottoStats);
                    returnValue = CalculateDistanceGeneralProbability(balls, 0, returnValue, game, dailyLottoStats);
                    break;
            }
            return returnValue;
        }

        private double CalculateDistanceGeneralProbability(int[] balls, int powerball, double returnValue, LottoGames game, StatsBaseClass baseStats)
        {
            for (int i = 0; i < 5; i++)
            {
                returnValue *= Math.Abs((double)baseStats.ballFrequency[balls[i] - 1] - baseStats.ballFrequencyAverage) / baseStats.ballFrequencyAverage;
            }
            if (game == LottoGames.powerball)
            {
                returnValue *= Math.Abs((double)baseStats.powerballFrequency[powerball - 1] - baseStats.powerballFrequencyAverage) / baseStats.powerballFrequencyAverage;
            }
            return returnValue;
        }
    }
}
