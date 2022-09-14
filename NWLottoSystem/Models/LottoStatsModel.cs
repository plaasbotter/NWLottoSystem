using static NWLottoSystem.Utils.Enums;

namespace NWLottoSystem.Models
{
    public class PowerBallStats
    {
        public List<LottoGames> games;
        public long count = 0;
        public int[] odds { get; set; }
        public int[] highs { get; set; }
        public int[] powerballFrequency { get; set; }
        public int[] ballFrequency { get; set; }
        public int[] sums { get; set; }
        public int highValue = 26;
        public string lottoTypeWhere = "";

        public double ballFrequencyAverage { get; set; }
        public double powerballFrequencyAverage { get; set; }
        public Dictionary<LottoGames, List<short>> lastResult { get; set; }
        public PowerBallStats()
        {
            odds = new int[6];
            highs = new int[6];
            powerballFrequency = new int[20];
            ballFrequency = new int[50];
            sums = new int[240];
            games = new List<LottoGames> { LottoGames.powerball, LottoGames.powerball_plus };
            lastResult = new Dictionary<LottoGames, List<short>>();
            foreach(var game in games)
            {
                lastResult.Add(game, new List<short>());
            }
            lottoTypeWhere = SharedStatsFunctions.GetLottoTypeWhereString(games);
        }
    }

    public class LottoStats
    {
        public List<LottoGames> games;
        public long count = 0;
        public int[] odds { get; set; }
        public int[] highs { get; set; }
        public int[] ballFrequency { get; set; }
        public int[] sums { get; set; } //343
        public double ballFrequencyAverage { get; set; }

        public int highValue = 27;
        public string lottoTypeWhere = "";
        public Dictionary<LottoGames, List<short>> lastResult { get; set; }
        public LottoStats()
        {
            odds = new int[8];
            highs = new int[8];
            ballFrequency = new int[52];
            sums = new int[343];
            games = new List<LottoGames> { LottoGames.lotto, LottoGames.lotto_plus_1, LottoGames.lotto_plus_2 };
            lastResult = new Dictionary<LottoGames, List<short>>();
            foreach (var game in games)
            {
                lastResult.Add(game, new List<short>());
            }
            lottoTypeWhere = SharedStatsFunctions.GetLottoTypeWhereString(games);
        }
    }

    public static class SharedStatsFunctions
    {
        public static string GetLottoTypeWhereString(List<LottoGames> games)
        {
            string lottoTypeWhere = "";
            foreach (var game in games)
            {
                lottoTypeWhere += $"\"lotto_type\" = {(byte)game} OR ";
            }
            return lottoTypeWhere.Substring(0, lottoTypeWhere.Length - 3);
        }
    }
}
