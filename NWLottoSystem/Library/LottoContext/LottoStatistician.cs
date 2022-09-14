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
        private DateTime _lastUpdateTime;

        public LottoStatistician(DatabaseContext dbContext, ILogger logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            powerBallStats = new PowerBallStats();
            lottoStats = new LottoStats();
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
                    using (FileStream fs = new FileStream($"{_lastUpdateTime.ToString("yyyy-MM-dd")}_pb_stats.json", FileMode.Create))
                    {
                        fs.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(powerBallStats)));
                    }
                    using (FileStream fs = new FileStream($"{_lastUpdateTime.ToString("yyyy-MM-dd")}_lt_stats.json", FileMode.Create))
                    {
                        fs.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(lottoStats)));
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
            UpdateLottoScores();
        }

        private void UpdateFrequencyAverages(LottoStats lottoStats)
        {
            foreach (var ball in lottoStats.ballFrequency)
            {
                lottoStats.ballFrequencyAverage += ball;
            }
            lottoStats.ballFrequencyAverage /= (double)lottoStats.ballFrequency.Length;
        }

        private void UpdateLottoScores()
        {

        }

        private void UpdatePowerBall()
        {
            _logger.Information("[{0}]", "LottoStatictician.UpdateLotto");
            _dbContext.UpdateCount(ref powerBallStats.count, powerBallStats.lottoTypeWhere);
            _dbContext.UpdateOdds(powerBallStats.odds, 5, false, powerBallStats.lottoTypeWhere);
            _dbContext.UpdateHighs(powerBallStats.highs, powerBallStats.highValue, 5, false, powerBallStats.lottoTypeWhere);
            _dbContext.UpdateBallFrequency(powerBallStats.ballFrequency, 5, false, powerBallStats.lottoTypeWhere);
            _dbContext.UpdateSums(powerBallStats.sums, 5, false, powerBallStats.lottoTypeWhere);
            _dbContext.UpdatePowerBallFrequency(powerBallStats.powerballFrequency, powerBallStats.lottoTypeWhere);
            _dbContext.GetLastResult(powerBallStats.lastResult, 5, true, powerBallStats.lottoTypeWhere, powerBallStats.games.Count);
            UpdateFrequencyAverages(powerBallStats);
            UpdatePowerballScores();
        }

        private void UpdateFrequencyAverages(PowerBallStats powerBallStats)
        {
            foreach (var ball in powerBallStats.ballFrequency)
            {
                powerBallStats.ballFrequencyAverage += ball;
            }
            powerBallStats.ballFrequencyAverage /= (double)powerBallStats.ballFrequency.Length;
            foreach (var ball in powerBallStats.powerballFrequency)
            {
                powerBallStats.powerballFrequencyAverage += ball;
            }
            powerBallStats.powerballFrequencyAverage /= (double)powerBallStats.powerballFrequency.Length;
        }

        private void UpdatePowerballScores()
        {

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

        public double TestHighProbability(int[] balls, int powerball, LottoGames game)
        {
            double returnValue = 0;
            switch (game)
            {
                case LottoGames.powerball:
                    returnValue = CalculatePowerballProbability(balls);
                    returnValue = CalculateHighPowerballProbability(balls, powerball, returnValue);
                    break;
                case LottoGames.lotto:
                    returnValue = CalculateLottoProbability(balls);
                    returnValue = CalculateHighLottoProbability(balls, returnValue);
                    break;
            }
            return returnValue;
        }

        private double CalculateHighLottoProbability(int[] balls, double returnValue)
        {
            foreach (var ball in balls)
            {
                returnValue *= (double)lottoStats.ballFrequency[ball - 1] / (double)lottoStats.count;
            }
            return returnValue;
        }

        private double CalculateHighPowerballProbability(int[] balls, int powerball, double returnValue)
        {
            for (int i = 0; i < 5; i++)
            {
                returnValue *= (double)powerBallStats.ballFrequency[balls[i] - 1] / (double)powerBallStats.count;
            }
            returnValue *= (double)powerBallStats.powerballFrequency[powerball - 1] / (double)powerBallStats.count;
            return returnValue;
        }

        public double TestLowProbability(int[] balls, int powerball, LottoGames game)
        {
            double returnValue = 0;
            switch (game)
            {
                case LottoGames.powerball:
                    returnValue = CalculatePowerballProbability(balls);
                    returnValue = CalculateLowPowerballProbability(balls, powerball, returnValue);
                    break;
                case LottoGames.lotto:
                    returnValue = CalculateLottoProbability(balls);
                    returnValue = CalculateLowLottoProbability(balls, returnValue);
                    break;
            }
            return returnValue;
        }

        private double CalculateLowLottoProbability(int[] balls, double returnValue)
        {
            double statVal = 0;
            foreach (var ball in balls)
            {
                statVal = 1.0 - ((double)lottoStats.ballFrequency[ball - 1] / (double)lottoStats.count);
                returnValue *= statVal;
            }
            return returnValue;
        }

        private double CalculateLowPowerballProbability(int[] balls, int powerball, double returnValue)
        {
            double statVal = 0;
            for (int i = 0; i < 5; i++)
            {
                statVal = 1.0 - ((double)powerBallStats.ballFrequency[balls[i] - 1] / (double)powerBallStats.count);
                returnValue *= statVal;
            }
            statVal = 1.0 - ((double)powerBallStats.powerballFrequency[powerball - 1] / (double)powerBallStats.count);
            returnValue *= statVal;
            return returnValue;
        }

        private double CalculateLottoProbability(int[] balls)
        {
            double statVal = 0;
            double returnValue = 1000000;
            int highs = 0;
            int odds = 0;
            int sum = 0;
            foreach (var ball in balls)
            {
                highs += (int)(ball / lottoStats.highValue);
                odds += ball % 2;
                sum += ball;
            }
            statVal = (double)lottoStats.sums[sum - 1] / (double)lottoStats.count;
            returnValue *= statVal;
            statVal *= (double)lottoStats.highs[highs] / (double)lottoStats.count;
            returnValue *= statVal;
            statVal *= (double)lottoStats.odds[odds] / (double)lottoStats.count;
            returnValue *= statVal;
            return returnValue;
        }

        private double CalculatePowerballProbability(int[] balls)
        {
            double returnValue = 1000000;
            int highs = 0;
            int odds = 0;
            int sum = 0;
            for (int i = 0; i < 5; i++)
            {
                highs += (int)(balls[i] / powerBallStats.highValue);
                odds += balls[i] % 2;
                sum += balls[i];
            }
            returnValue *= (double)powerBallStats.sums[sum - 1] / (double)powerBallStats.count;
            returnValue *= (double)powerBallStats.highs[highs] / (double)powerBallStats.count;
            returnValue *= (double)powerBallStats.odds[odds] / (double)powerBallStats.count;
            return returnValue;
        }

        public double TestDistanceProbability(int[] balls, int powerball, LottoGames game)
        {
            double returnValue = 0;
            switch (game)
            {
                case LottoGames.powerball:
                    returnValue = CalculatePowerballProbability(balls);
                    returnValue = CalculateDistancePowerballProbability(balls, powerball, returnValue);
                    break;
                case LottoGames.lotto:
                    returnValue = CalculateLottoProbability(balls);
                    returnValue = CalculateDistanceLottoProbability(balls, returnValue);
                    break;
            }
            return returnValue;
        }

        private double CalculateDistanceLottoProbability(int[] balls, double returnValue)
        {
            foreach (var ball in balls)
            {
                returnValue *= Math.Abs((double)lottoStats.ballFrequency[ball - 1] - lottoStats.ballFrequencyAverage) / lottoStats.ballFrequencyAverage;
            }
            return returnValue;
        }

        private double CalculateDistancePowerballProbability(int[] balls, int powerball, double returnValue)
        {
            for (int i = 0; i < 5; i++)
            {
                //returnValue *= (double)powerBallStats.ballFrequency[balls[i] - 1] / (double)powerBallStats.count;
                returnValue *= Math.Abs((double)powerBallStats.ballFrequency[balls[i] - 1] - powerBallStats.ballFrequencyAverage) / powerBallStats.ballFrequencyAverage;
            }
            //returnValue *= (double)powerBallStats.powerballFrequency[powerball - 1] / (double)powerBallStats.count;
            returnValue *= Math.Abs((double)powerBallStats.powerballFrequency[powerball - 1] - powerBallStats.powerballFrequencyAverage) / powerBallStats.powerballFrequencyAverage;
            return returnValue;
        }
    }
}
