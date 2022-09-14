using AngleSharp;
using Newtonsoft.Json;
using NWLottoSystem.Models;
using NWLottoSystem.Utils;
using Serilog;
using static NWLottoSystem.Utils.Enums;

namespace NWLottoSystem.Library.LottoContext
{
    public class LottoResult
    {
        private short[] Balls;
        private short ExtraBall;
        private readonly DatabaseContext _dbContext;
        private readonly ILogger _logger;
        private readonly HttpClient _client;

        public LottoResult(ILogger logger, DatabaseContext dbContext, HttpClient client)
        {
            _logger = logger;
            _dbContext = dbContext;
            _client = client;
        }

        public void PopulateResultsFromHistorian(DateTime inputDate, LottoGames lottoGame)
        {
            Balls = new short[6];
            ExtraBall = 0;
            try
            {
                string URL = GetLottoURLFromHistorian(lottoGame, inputDate);
                string html = "";
                using (HttpClient client = new HttpClient())
                {
                    // html = client.DownloadString(URL);
                    html = client.GetStringAsync(URL).Result;
                }
                var config = Configuration.Default;
                var context = BrowsingContext.New(config);
                var document = context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();
                var body = document.Body.ChildNodes[7];
                var wholeBody = body.ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes[1];
                LoadBallsFromHistorian(lottoGame, wholeBody.ChildNodes[1]);
                _dbContext.InsertLottoResults(Balls, ExtraBall, inputDate, lottoGame);
            }
            catch (Exception err)
            {
                _logger.Error("[{0}] [{1}]", "LottoResult.PopulateResults", err.Message);
            }
        }

        private void LoadBallsFromHistorian(LottoGames lottoGame, AngleSharp.Dom.INode input)
        {
            var wholeBodyTopBalls = input.ChildNodes[1].ChildNodes[5].ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes[3].ChildNodes[1];
            int counter = 0;
            int maxNodes = 0;
            switch (lottoGame)
            {
                case LottoGames.powerball:
                    maxNodes = 5;
                    break;
                case LottoGames.powerball_plus:
                    maxNodes = 5;
                    break;
                case LottoGames.lotto:
                    maxNodes = 6;
                    break;
                case LottoGames.lotto_plus_1:
                    maxNodes = 6;
                    break;
                case LottoGames.lotto_plus_2:
                    maxNodes = 6;
                    break;
                default:
                    break;
            }
            for (int i = 1; i < wholeBodyTopBalls.ChildNodes.Length; i += 2)
            {
                if (counter == maxNodes)
                {
                    ExtraBall = short.Parse(wholeBodyTopBalls.ChildNodes[i].ChildNodes[1].ChildNodes[0].NodeValue);
                }
                else
                {
                    Balls[counter] = short.Parse(wholeBodyTopBalls.ChildNodes[i].ChildNodes[1].ChildNodes[0].NodeValue);
                }
                counter++;
            }
        }

        private void LoadBallsFromCurrentorian(LottoGames lottoGame, string rawInput)
        {
            switch (lottoGame)
            {
                case LottoGames.powerball:
                    LoadAndInsertBallsPowerballFromCurrentorain(lottoGame, rawInput);
                    break;
                case LottoGames.powerball_plus:
                    LoadAndInsertBallsPowerballFromCurrentorain(lottoGame, rawInput);
                    break;
                case LottoGames.lotto:
                    LoadAndInsertBallsLottoFromCurrentorain(lottoGame, rawInput);
                    break;
                case LottoGames.lotto_plus_1:
                    LoadAndInsertBallsLottoFromCurrentorain(lottoGame, rawInput);
                    break;
                case LottoGames.lotto_plus_2:
                    LoadAndInsertBallsLottoFromCurrentorain(lottoGame, rawInput);
                    break;
                default:
                    return;
            }
        }

        private void LoadAndInsertBallsLottoFromCurrentorain(LottoGames lottoGame, string rawInput)
        {
            LottoAPIResultsModel resultsModel = JsonConvert.DeserializeObject<LottoAPIResultsModel>(rawInput);
            bool goOn = true;
            if (resultsModel.message != "No Record Found")
            {
                DateTime inputDate = DateTime.MinValue;
                foreach (var datum in resultsModel.data)
                {
                    goOn = true;
                    Balls[0] = short.Parse(datum.ball1);
                    Balls[1] = short.Parse(datum.ball2);
                    Balls[2] = short.Parse(datum.ball3);
                    Balls[3] = short.Parse(datum.ball4);
                    Balls[4] = short.Parse(datum.ball5);
                    Balls[5] = short.Parse(datum.ball6);
                    ExtraBall = short.Parse(datum.bonusBall);
                    for (int i = 0; i < 6; i++)
                    {
                        if (Balls[i] == 0)
                        {
                            goOn = false;
                        }
                    }
                    if (ExtraBall == 0)
                    {
                        goOn = false;
                    }
                    inputDate = DateTime.Parse(datum.drawDate);
                    if (goOn)
                    {
                        _dbContext.InsertLottoResults(Balls, ExtraBall, inputDate, lottoGame);
                    }
                    else
                    {
                        _logger.Error("[{0}] Erronous Ball [{1}] [{2}] [{3}] [{4}]", "LottoResult.LoadAndInsertBallsLottoFromCurrentorain", Balls, ExtraBall, inputDate, lottoGame);
                    }
                }
            }
        }

        private void LoadAndInsertBallsPowerballFromCurrentorain(LottoGames lottoGame, string rawInput)
        {
            PowerballAPIResultsModel resultsModel = JsonConvert.DeserializeObject<PowerballAPIResultsModel>(rawInput);
            bool goOn = true;
            if (resultsModel.message != "No Record Found")
            {
                DateTime inputDate = DateTime.MinValue;
                foreach (var datum in resultsModel.data)
                {
                    goOn = true;
                    Balls[0] = short.Parse(datum.ball1);
                    Balls[1] = short.Parse(datum.ball2);
                    Balls[2] = short.Parse(datum.ball3);
                    Balls[3] = short.Parse(datum.ball4);
                    Balls[4] = short.Parse(datum.ball5);
                    Balls[5] = 0;
                    ExtraBall = short.Parse(datum.powerball);
                    for (int i = 0; i < 5; i++)
                    {
                        if (Balls[i] == 0)
                        {
                            goOn = false;
                        }
                    }    
                    if (ExtraBall == 0)
                    {
                        goOn = false;
                    }
                    inputDate = DateTime.Parse(datum.drawDate);
                    if (goOn)
                    {
                        _dbContext.InsertLottoResults(Balls, ExtraBall, inputDate, lottoGame);
                    }
                    else
                    {
                        _logger.Error("[{0}] Erronous Ball [{1}] [{2}] [{3}] [{4}]", "LottoResult.LoadAndInsertBallsPowerballFromCurrentorain", Balls, ExtraBall, inputDate, lottoGame);
                    }
                }
            }
        }

        public void PopulateResultsFromCurrentorian(LottoAPISearchPattern lottoAPISearchPattern)
        {
            _logger.Information("[{0}] [{1}]", "LottoResult.PopulateResultsFromCurrentorian", JsonConvert.SerializeObject(lottoAPISearchPattern));
            Balls = new short[6];
            ExtraBall = 0;
            try
            {
                string gamename = "";
                switch(lottoAPISearchPattern.lottoGame)
                {
                    case LottoGames.powerball:
                        gamename = "POWERBALL";
                        break;
                    case LottoGames.powerball_plus:
                        gamename = "POWERBALLPLUS";
                        break;
                    case LottoGames.lotto:
                        gamename = "LOTTO";
                        break;
                    case LottoGames.lotto_plus_1:
                        gamename = "LOTTOPLUS";
                        break;
                    case LottoGames.lotto_plus_2:
                        gamename = "LOTTOPLUS2";
                        break;
                    default:
                        return;
                }
                Dictionary<string, string> data = new Dictionary<string, string>
                {
                    {"gameName", gamename },
                    {"offset","0" },
                    {"limit",lottoAPISearchPattern.results.ToString() },
                    {"startDate", lottoAPISearchPattern.startDate.ToString("dd/MM/yyyy") },
                    {"endDate", lottoAPISearchPattern.endDate.ToString("dd/MM/yyyy") }
                };
                string rawResponse = "";
                using (HttpContent formContent = new FormUrlEncodedContent(data))
                {
                    using (HttpResponseMessage response = _client.PostAsync(Config.configVaraibles.lottoAPIURL, formContent).GetAwaiter().GetResult())
                    {
                        response.EnsureSuccessStatusCode();
                        rawResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
                LoadBallsFromCurrentorian(lottoAPISearchPattern.lottoGame, rawResponse);
            }
            catch (Exception err)
            {
                _logger.Error("[{0}] [{1}]", "LottoResult.PopulateResultsFromCurrentorian", err.Message);
            }
        }

        private string GetLottoURLFromHistorian(LottoGames lottoGame, DateTime lottoData)
        {
            string returnValue = @"https://www.lotteryresults.co.za/";
            switch (lottoGame)
            {
                case LottoGames.powerball:
                    returnValue += @"powerball/";
                    break;
                case LottoGames.powerball_plus:
                    returnValue += @"powerball-plus/";
                    break;
                case LottoGames.lotto:
                    returnValue += @"lotto/";
                    break;
                case LottoGames.lotto_plus_1:
                    returnValue += @"lotto-plus/";
                    break;
                case LottoGames.lotto_plus_2:
                    returnValue += @"lotto-plus/";
                    break;
                default:
                    break;
            }
            returnValue += "results-" + lottoData.ToString("dd-MMM-yyyy");
            return returnValue.ToLower();
        }
    }
}
