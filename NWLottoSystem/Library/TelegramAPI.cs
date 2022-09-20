using Newtonsoft.Json;
using NWLottoSystem.Library.LottoContext;
using NWLottoSystem.Models;
using NWLottoSystem.Utils;
using Serilog;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static NWLottoSystem.Utils.Enums;

namespace NWLottoSystem.Library
{
    public class TelegramAPI
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;
        private readonly DatabaseContext _dbContext;
        private readonly LottoGenerator _lottoGenerator;
        private readonly LottoStatistician _lottoStatistician;
        private long _lastmessageupdate;
        private TelegramRoot _responses;


        public TelegramAPI(ILogger logger, HttpClient client, DatabaseContext dbContext, LottoGenerator lottoGenerator, LottoStatistician lottoStatistician)
        {
            _logger = logger;
            _client = client;
            _dbContext = dbContext;
            _lottoGenerator = lottoGenerator;
            _lottoStatistician = lottoStatistician;
            _lastmessageupdate = _dbContext.GetLastMessageUpdate();
        }

        public void ScanForMessages()
        {
            string rawResponse = "";
            try
            {
                var urlString = string.Format(@"https://api.telegram.org/bot{0}/getUpdates", Config.configVaraibles.TelegramAPIToken);
                using (HttpResponseMessage response = _client.GetAsync(urlString).GetAwaiter().GetResult())
                {
                    rawResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                _responses = JsonConvert.DeserializeObject<TelegramRoot>(rawResponse);
            }
            catch (Exception err)
            {
                _logger.Error(err, "[{0}]", "TelegramAPI.ScanForMessages");
            }
        }

        public void ActionResponses()
        {
            if (_responses.ok == true)
            {
                foreach (var result in _responses.result)
                {
                    if (result.message.message_id > _lastmessageupdate)
                    {
                        if (result.message.entities != null && result.message.entities.Length == 1)
                        {
                            if (result.message.entities[0].type == "bot_command")
                            {
                                switch (result.message.text)
                                {
                                    case "/get_lotto":
                                        SendMessage(result.message.from.id, _lottoGenerator.GetBestLowLotto(1235813));
                                        SendMessage(result.message.from.id, _lottoGenerator.GetBestHighLotto(1235813));
                                        SendMessage(result.message.from.id, _lottoGenerator.GetBestDistanceLotto(1235813));
                                        break;
                                    case "/get_powerball":
                                        SendMessage(result.message.from.id, _lottoGenerator.GetBestLowPowerball(1235813));
                                        SendMessage(result.message.from.id, _lottoGenerator.GetBestHighPowerball(1235813));
                                        SendMessage(result.message.from.id, _lottoGenerator.GetBestDistancePowerball(1235813));
                                        break;
                                    case "/get_daily":
                                        SendMessage(result.message.from.id, _lottoGenerator.GetBestLowDaily(1235813));
                                        SendMessage(result.message.from.id, _lottoGenerator.GetBestHighDaily(1235813));
                                        SendMessage(result.message.from.id, _lottoGenerator.GetBestDistanceDaily(1235813));
                                        break;
                                    case "/get_last_lotto_result":
                                        SendMessage(result.message.from.id, _lottoStatistician.GetLastLottoResult());
                                        break;
                                    case "/get_last_powerball_result":
                                        SendMessage(result.message.from.id, _lottoStatistician.GetLastPowerballResult());
                                        break;
                                    case "/get_last_daily_result":
                                        SendMessage(result.message.from.id, _lottoStatistician.GetLastDailyResult());
                                        break;
                                    default:
                                        break;
                                }
                            }

                        }
                        else
                        {
                            TryExamineMessage(result.message);
                        }
                        _lastmessageupdate = (long)result.message.message_id;
                        _dbContext.InsertTelegramMessage(_lastmessageupdate, result.message.text, result.message.from.id);
                    }
                }
            }
        }

        public void CheckEntries()
        {
            List<LottoEntry> entries = new List<LottoEntry>();
            _dbContext.LoadEntries(entries);
            foreach (var entry in entries)
            {
                StringBuilder sb = new StringBuilder();
                if (entry.games == LottoGames.powerball)
                {
                    sb.AppendLine(CheckAndSendEntry(entry, LottoGames.powerball));
                    sb.AppendLine(CheckAndSendEntry(entry, LottoGames.powerball_plus));
                }
                if (entry.games == LottoGames.lotto)
                {
                    sb.AppendLine(CheckAndSendEntry(entry, LottoGames.lotto));
                    sb.AppendLine(CheckAndSendEntry(entry, LottoGames.lotto_plus_1));
                    sb.AppendLine(CheckAndSendEntry(entry, LottoGames.lotto_plus_2));
                }
                string result = sb.ToString();
                if (string.IsNullOrEmpty(result) == false && result.Replace("\n", "").Replace("\r", "").Length > 0)
                {
                    SendMessage(entry.sender_id, result);
                    _dbContext.UpdateLottoEntryToChecked(entry);
                }
            }
        }


        private string CheckAndSendEntry(LottoEntry entry, LottoGames game)
        {
            List<short> numbersForThatDay = _dbContext.GetDrawResult(entry.timestamp, game);
            string returnValue = string.Empty;
            if (numbersForThatDay.Count == 7)
            {
                int sameZies = 0;
                foreach (var entryNumber in entry.numbers[0])
                {
                    foreach (var resultNumber in numbersForThatDay)
                    {
                        if (entryNumber == resultNumber)
                        {
                            sameZies++;
                        }
                    }
                }
                returnValue = $"{game} on {entry.timestamp}: {sameZies} match(es)";
            }
            return returnValue;
        }

        private void TryExamineMessage(Message message)
        {
            try
            {
                if (message.text.StartsWith("Standard Bank"))
                {
                    List<LottoEntry> lottoEntries = new List<LottoEntry>();
                    LottoGames game = LottoGames.unknown;
                    Regex rg;
                    MatchCollection mt;
                    rg = new Regex(@"\w{5,9}");
                    mt = rg.Matches(message.text);
                    foreach (var match in mt.ToList())
                    {
                        if (match.Value == "Powerball")
                        {
                            game = LottoGames.powerball;
                        }
                        if (match.Value == "Lotto")
                        {
                            game = LottoGames.lotto;
                        }
                    }
                    if (game == LottoGames.unknown)
                    {
                        throw new Exception("Unknown lotto game");
                    }
                    rg = new Regex(@"\d draw");
                    mt = rg.Matches(message.text);
                    int numGames = int.Parse(mt.ToList()[0].Value.Split(" ")[0]);
                    for (int i = 0; i < numGames; i++)
                    {
                        lottoEntries.Add(new LottoEntry { sender_id = message.from.id, games = game });
                    }
                    rg = new Regex(@"\d{2} \d{2} \d{2} \d{2} \d{2} \d{2}");
                    mt = rg.Matches(message.text);
                    foreach (var entry in lottoEntries)
                    {
                        foreach (var match in mt.ToList())
                        {
                            List<short> tempList = new List<short>();
                            var splitted = match.Value.Split(" ");
                            foreach (var split in splitted)
                            {
                                tempList.Add(short.Parse(split));
                            }
                            entry.numbers.Add(tempList);
                        }
                    }
                    rg = new Regex(@"\w{3}\d{8,}");
                    mt = rg.Matches(message.text);
                    foreach (var entry in lottoEntries)
                    {
                        entry.reference = mt.ToList().First().Value;
                    }
                    rg = new Regex(@"\d{2}/\d{2}/\d{4}");
                    mt = rg.Matches(message.text);
                    var mtList = mt.ToList();
                    for (int i = 0; i < numGames; i++)
                    {
                        lottoEntries[i].timestamp = DateTime.ParseExact(mtList[i].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }
                    foreach (var entry in lottoEntries)
                    {
                        _dbContext.SubmitEntry(entry);
                    }
                }
                else
                {
                    throw new Exception("Message type unknown");
                }
            }
            catch (Exception err)
            {
                _logger.Error(err, "[{0}] Could not interpret message [{1}] [{2}]", "TelegramAPI.TryExamineMessage", message.message_id, message.text);
            }
        }

        public void SendMessage(int chat_id, string text)
        {
            string urlString = string.Empty;
            try
            {
                urlString = string.Format(@"https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}", Config.configVaraibles.TelegramAPIToken, chat_id, text);
                using (HttpResponseMessage response = _client.GetAsync(urlString).GetAwaiter().GetResult())
                {
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception err)
            {
                _logger.Error(err, "[{0}] [{1}]", "TelegramAPI.SendMessage", urlString);
            }
        }
    }
}
