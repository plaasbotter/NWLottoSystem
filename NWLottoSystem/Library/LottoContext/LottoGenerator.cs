using Serilog;
using System.Text;

namespace NWLottoSystem.Library.LottoContext
{

    public class LottoGenerator
    {
        private ILogger _logger;
        private LottoStatistician _staticitican;
        private Random rando = new Random();

        public LottoGenerator(ILogger logger, LottoStatistician staticitican)
        {
            _logger = logger;
            _staticitican = staticitican;
        }

        public string GetBestHighLotto(int maxItterations)
        {
            List<List<int>> returnValue = new List<List<int>>();
            double bestScore = double.MinValue;
            List<int> Lotto = new List<int>();
            for (int i = 0; i < maxItterations; i++)
            {
                List<int> tempLotto = GenerateLotto();
                double tempScore = _staticitican.TestHighProbability(tempLotto.ToArray(), 0, Utils.Enums.LottoGames.lotto);
                if (tempScore >= bestScore)
                {
                    _logger.Debug("[{0}] Found better [{1}] [{2}]", "LottoGenerator.GetBestHighLotto", tempScore, tempLotto);
                    bestScore = tempScore;
                    Lotto = tempLotto;
                    returnValue.Add(Lotto);
                }
            }
            while (returnValue.Count > 3)
            {
                returnValue.RemoveAt(0);
            }
            returnValue.Reverse();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Best High Lotto:");
            for (int i = 0; i < returnValue.Count; i++)
            {
                sb.AppendLine($"{i + 1}=>{string.Join(" ", returnValue[i])}");
            }
            return sb.ToString();
        }

        public string GetBestLowLotto(int maxItterations)
        {
            List<List<int>> returnValue = new List<List<int>>();
            double bestScore = double.MinValue;
            List<int> Lotto = new List<int>();
            for (int i = 0; i < maxItterations; i++)
            {
                List<int> tempLotto = GenerateLotto();
                double tempScore = _staticitican.TestLowProbability(tempLotto.ToArray(), 0, Utils.Enums.LottoGames.lotto);
                if (tempScore >= bestScore)
                {
                    _logger.Debug("[{0}] Found better [{1}] [{2}]", "LottoGenerator.GetBestLowLotto", tempScore, tempLotto);
                    bestScore = tempScore;
                    Lotto = tempLotto;
                    returnValue.Add(Lotto);
                }
            }
            while (returnValue.Count > 3)
            {
                returnValue.RemoveAt(0);
            }
            returnValue.Reverse();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Best Low Lotto:");
            for (int i = 0; i < returnValue.Count; i++)
            {
                sb.AppendLine($"{i + 1}=>{string.Join(" ", returnValue[i])}");
            }
            return sb.ToString();
        }

        public string GetBestHighPowerball(int maxItterations)
        {
            List<List<int>> returnValue = new List<List<int>>();
            double bestScore = double.MinValue;
            List<int> powerball = new List<int>();
            for (int i = 0; i < maxItterations; i++)
            {
                List<int> tempPowerBall = GeneratePowerball();
                double tempScore = _staticitican.TestHighProbability(tempPowerBall.ToArray(), tempPowerBall[5], Utils.Enums.LottoGames.powerball);
                if (tempScore >= bestScore)
                {
                    _logger.Debug("[{0}] Found better [{1}] [{2}]", "LottoGenerator.GetBestHighPowerball", tempScore, tempPowerBall);
                    bestScore = tempScore;
                    powerball = tempPowerBall;
                    returnValue.Add(powerball);
                }
            }
            while (returnValue.Count > 3)
            {
                returnValue.RemoveAt(0);
            }
            returnValue.Reverse();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Best High Powerball:");
            for (int i = 0; i < returnValue.Count; i++)
            {
                sb.AppendLine($"{i + 1}=>{string.Join(" ", returnValue[i])}");
            }
            return sb.ToString();
        }

        public string GetBestLowPowerball(int maxItterations)
        {
            List<List<int>> returnValue = new List<List<int>>();
            double bestScore = double.MinValue;
            List<int> powerball = new List<int>();
            for (int i = 0; i < maxItterations; i++)
            {
                List<int> tempPowerBall = GeneratePowerball();
                double tempScore = _staticitican.TestLowProbability(tempPowerBall.ToArray(), tempPowerBall[5], Utils.Enums.LottoGames.powerball);
                if (tempScore >= bestScore)
                {
                    _logger.Debug("[{0}] Found better [{1}] [{2}]", "LottoGenerator.GetBestLowPowerball", tempScore, tempPowerBall);
                    bestScore = tempScore;
                    powerball = tempPowerBall;
                    returnValue.Add(powerball);
                }
            }
            while (returnValue.Count > 3)
            {
                returnValue.RemoveAt(0);
            }
            returnValue.Reverse();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Best Low Powerball:");
            for (int i = 0; i < returnValue.Count; i++)
            {
                sb.AppendLine($"{i + 1}=>{string.Join(" ", returnValue[i])}");
            }
            return sb.ToString();
        }

        private List<int> GenerateLotto()
        {
            List<int> returnValue = new List<int>();
            int newVal = 0;
            while (returnValue.Count < 6)
            {
                newVal = rando.Next(1, 53);
                if (returnValue.Contains(newVal) == false)
                {
                    returnValue.Add(newVal);
                }
            }
            return returnValue;
        }

        private List<int> GeneratePowerball()
        {
            List<int> returnValue = new List<int>();
            int newVal = 0;
            while (returnValue.Count < 5)
            {
                newVal = rando.Next(1, 51);
                if (returnValue.Contains(newVal) == false)
                {
                    returnValue.Add(newVal);
                }
            }
            returnValue.Add(rando.Next(1, 20));
            return returnValue;
        }

        public string GetBestDistanceLotto(int maxItterations)
        {
            List<List<int>> returnValue = new List<List<int>>();
            double bestScore = double.MinValue;
            List<int> Lotto = new List<int>();
            for (int i = 0; i < maxItterations; i++)
            {
                List<int> tempLotto = GenerateLotto();
                double tempScore = _staticitican.TestDistanceProbability(tempLotto.ToArray(), 0, Utils.Enums.LottoGames.lotto);
                if (tempScore >= bestScore)
                {
                    _logger.Debug("[{0}] Found better [{1}] [{2}]", "LottoGenerator.GetBestDistanceLotto", tempScore, tempLotto);
                    bestScore = tempScore;
                    Lotto = tempLotto;
                    returnValue.Add(Lotto);
                }
            }
            while (returnValue.Count > 3)
            {
                returnValue.RemoveAt(0);
            }
            returnValue.Reverse();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Best Distance Lotto:");
            for (int i = 0; i < returnValue.Count; i++)
            {
                sb.AppendLine($"{i + 1}=>{string.Join(" ", returnValue[i])}");
            }
            return sb.ToString();
        }

        public string GetBestDistancePowerball(int maxItterations)
        {
            List<List<int>> returnValue = new List<List<int>>();
            double bestScore = double.MinValue;
            List<int> powerball = new List<int>();
            for (int i = 0; i < maxItterations; i++)
            {
                List<int> tempPowerBall = GeneratePowerball();
                double tempScore = _staticitican.TestDistanceProbability(tempPowerBall.ToArray(), tempPowerBall[5], Utils.Enums.LottoGames.powerball);
                if (tempScore >= bestScore)
                {
                    _logger.Debug("[{0}] Found better [{1}] [{2}]", "LottoGenerator.GetBestDistancePowerball", tempScore, tempPowerBall);
                    bestScore = tempScore;
                    powerball = tempPowerBall;
                    returnValue.Add(powerball);
                }
            }
            while (returnValue.Count > 3)
            {
                returnValue.RemoveAt(0);
            }
            returnValue.Reverse();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Best Distance Powerball:");
            for (int i = 0; i < returnValue.Count; i++)
            {
                sb.AppendLine($"{i + 1}=>{string.Join(" ", returnValue[i])}");
            }
            return sb.ToString();
        }
    }
}
