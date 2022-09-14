using Npgsql;
using NWLottoSystem.Models;
using NWLottoSystem.Utils;
using Serilog;
using static NWLottoSystem.Utils.Enums;

namespace NWLottoSystem.Library
{

    public class DatabaseContext
    {
        private readonly ILogger _logger;
        private readonly object _connectionLock = new object();
        private readonly NpgsqlConnection _con;

        public DatabaseContext(ILogger logger, string connectionString)
        {
            _logger = logger;
            //_connectionString = connectionString;
            _con = new NpgsqlConnection(connectionString);
            _con.OpenAsync().Wait();
        }

        public void InsertLottoResults(short[] balls, short extraBall, DateTime inputDate, LottoGames lottoGame)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                string query = "INSERT INTO \"lotto_results\" (\"timestamp\", \"num_1\", \"num_2\", \"num_3\", \"num_4\", \"num_5\", \"num_6\", \"num_e\", \"lotto_type\") VALUES (@timestamp, @num_1, @num_2, @num_3, @num_4, @num_5, @num_6, @num_e, @lotto_type) ON CONFLICT (\"timestamp\", \"lotto_type\") DO NOTHING;";
                NpgsqlParameter[] parameters = new NpgsqlParameter[9];
                parameters[0] = new NpgsqlParameter("@timestamp", inputDate);
                for (int i = 0; i < balls.Length; i++)
                {
                    parameters[i + 1] = new NpgsqlParameter($"@num_{i + 1}", balls[i]);
                }
                parameters[7] = new NpgsqlParameter("@num_e", extraBall);
                parameters[8] = new NpgsqlParameter("@lotto_type", (short)lottoGame);
                using (NpgsqlCommand com = new NpgsqlCommand(query, _con))
                {
                    com.Parameters.AddRange(parameters);
                    int dbReturn = com.ExecuteNonQuery();
                    if (dbReturn == 1)
                    {
                        _logger.Information("INSERTED: [{0}] [{1}] [{2}] [{3}]", balls, extraBall, inputDate, lottoGame);
                    }
                    else if (dbReturn == 0)
                    {
                        _logger.Warning("NON INSERT! [{0}] [{1}] [{2}] [{3}]", balls, extraBall, inputDate, lottoGame);
                    }
                    else
                    {
                        _logger.Error("ERROR [{0}] [{1}] [{2}] [{3}]", balls, extraBall, inputDate, lottoGame);
                    }
                }
            }
        }

        internal void GetLastResult(Dictionary<LottoGames, List<short>> lastResult, int amountOfNumbers, bool includeBonusBall, string lottoTypeWhere, int limit)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                List<string> nums = new List<string>();
                for (int i = 0; i < amountOfNumbers; i++)
                {
                    nums.Add($"\"num_{i + 1}\"");
                }
                if (includeBonusBall)
                {
                    nums.Add($"\"num_e\"");
                }
                string query = $"SELECT \"lotto_type\", {string.Join(",", nums)} FROM \"lotto_results\" WHERE {lottoTypeWhere} ORDER BY \"timestamp\" DESC LIMIT {limit}";
                using (NpgsqlCommand com = new NpgsqlCommand(query, _con))
                {
                    using (NpgsqlDataReader rdr = com.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            LottoGames game = (LottoGames)rdr.GetInt16(0);
                            lastResult[game].Clear();
                            for (int i = 0; i < nums.Count; i++)
                            {
                                lastResult[game].Add(rdr.GetInt16(i + 1));
                            }
                        }
                    }
                }
            }
        }

        public long GetLastMessageUpdate()
        {
            long returnValue = 0;
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                string query = "SELECT MAX(\"Id\") FROM \"telegram_messages\"";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    object? tempValue = cmd.ExecuteScalar();
                    if (tempValue != null && tempValue.GetType() == typeof(long))
                    {
                        returnValue = (long)tempValue;
                    }
                }
            }
            return returnValue;
        }

        internal void LoadEntries(List<LottoEntry> entries)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                string query = "SELECT * FROM \"lotto_entries\" WHERE \"checked\" is FALSE";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            LottoEntry entry = new LottoEntry();
                            entry.id = rdr.GetInt32(0);
                            List<short> numbers = new List<short>();
                            for (int i = 0; i < 6; i++)
                            {
                                numbers.Add(rdr.GetInt16(i + 1));
                            }
                            entry.numbers.Add(numbers);
                            entry.games = (LottoGames)rdr.GetInt16(7);
                            entry.isChecked = rdr.GetBoolean(8);
                            entry.reference = rdr.GetString(9);
                            entry.sender_id = rdr.GetInt32(10);
                            entry.timestamp = rdr.GetDateTime(11);
                            entries.Add(entry);
                        }
                    }
                }
            }
        }

        internal void UpdateLottoEntryToChecked(LottoEntry entry)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                string query = "UPDATE \"lotto_entries\" SET \"checked\" = TRUE WHERE \"reference\" = @reference AND \"id\" = @id;";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query,_con))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("@reference", entry.reference));
                    cmd.Parameters.Add(new NpgsqlParameter("@id", entry.id));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        internal List<short> GetDrawResult(DateTime timestamp, LottoGames game)
        {
            List<short> returnValue = new List<short>();
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                string query = "SELECT * FROM \"lotto_results\" WHERE \"timestamp\" = @timestamp AND \"lotto_type\" = @lotto_type";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("@timestamp", timestamp));
                    cmd.Parameters.Add(new NpgsqlParameter("@lotto_type", (short)game));
                    using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                returnValue.Add(rdr.GetInt16(i + 1));
                            }
                            returnValue.Add(rdr.GetInt16(8));
                        }

                    }
                }
            }
            return returnValue;
        }

        internal void InsertTelegramMessage(long lastmessageupdate, string text, int from)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                string query = "INSERT INTO \"telegram_messages\" (\"Id\", \"message\", \"From\") VALUES (@id,@message,@from);";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("@id", lastmessageupdate));
                    cmd.Parameters.Add(new NpgsqlParameter("@message", text));
                    cmd.Parameters.Add(new NpgsqlParameter("@from", from));
                    _logger.Information("[{0}] Commited [{1}] to DB", "DatabaseContext.InsertTelegramMessage", cmd.ExecuteNonQuery());
                }
            }
        }

        public void UpdateSums(int[] sums, int amountOfNumbers, bool includeBonusBall, string lottoTypeWhere)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                List<string> nums = new List<string>();
                for (int i = 0; i < amountOfNumbers; i++)
                {
                    nums.Add($"\"num_{i + 1}\"");
                }
                if (includeBonusBall)
                {
                    nums.Add($"\"num_e\"");
                }
                string query = $"SELECT ({string.Join(" + ", nums)}) AS sums, COUNT(*) FROM \"lotto_results\" WHERE {lottoTypeWhere} GROUP BY sums";
                using (NpgsqlCommand com = new NpgsqlCommand(query, _con))
                {
                    using (NpgsqlDataReader rdr = com.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            int position = rdr.GetInt32(0);
                            if (includeBonusBall)
                            {
                                position = (int)Math.Round(((double)position / 7 * 6), 0);
                            }
                            sums[position] += (int)rdr.GetInt64(1);
                        }
                    }
                }
            }
            _logger.Information("[{0}] [{1}]", "DatabaseContext.UpdateSums", sums);
        }

        public void SubmitEntry(LottoEntry entry)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                List<string> nums = new List<string>();
                for (int i = 0; i < 6; i++)
                {
                    nums.Add($"num_{i + 1}");
                }
                string query = $"INSERT INTO \"lotto_entries\" (\"timestamp\", \"{string.Join("\",\"", nums)}\", \"lotto_type\", \"checked\", \"reference\", \"sender_id\") VALUES (@timestamp, @{string.Join(",@", nums)}, @lotto_type, @checked, @reference, @sender_id)";
                foreach (var numbers in entry.numbers)
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                    {
                        cmd.Parameters.Add(new NpgsqlParameter("@timestamp", entry.timestamp));
                        for (int i = 0; i < numbers.Count; i++)
                        {
                            cmd.Parameters.Add(new NpgsqlParameter($"@num_{i + 1}", numbers[i]));
                        }
                        cmd.Parameters.Add(new NpgsqlParameter("@lotto_type", (short)entry.games));
                        cmd.Parameters.Add(new NpgsqlParameter("@checked", entry.isChecked));
                        cmd.Parameters.Add(new NpgsqlParameter("@reference", entry.reference));
                        cmd.Parameters.Add(new NpgsqlParameter("@sender_id", entry.sender_id));
                        int dbReturn = cmd.ExecuteNonQuery();
                        _logger.Information("[{0}] [{1}]", "DatabaseContext.SubmitEntry", $"Inserted {dbReturn} entries");
                    }
                }
            }
        }

        public void UpdateCount(ref long count, string lottoTypeWhere)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                string query = $"SELECT COUNT(*) FROM \"lotto_results\" WHERE {lottoTypeWhere};";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    long tempValue = (long)cmd.ExecuteScalar();
                    if (tempValue > 0)
                    {
                        count = tempValue;
                    }
                }
            }
            _logger.Information("[{0}] [{1}]", "DatabaseContext.UpdateCount", count);
        }

        public void UpdatePowerBallFrequency(int[] powerballFrequency, string lottoTypeWhere)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                string query = $"SELECT \"num_e\" AS nums, COUNT(*) FROM \"lotto_results\" WHERE {lottoTypeWhere} GROUP BY nums";
                using (NpgsqlCommand com = new NpgsqlCommand(query, _con))
                {
                    using (NpgsqlDataReader rdr = com.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (rdr.GetInt32(0) != 0)
                            {
                                powerballFrequency[rdr.GetInt32(0) - 1] = (int)rdr.GetInt64(1);
                            }
                        }
                    }
                }
            }
            _logger.Information("[{0}] [{1}]", "DatabaseContext.UpdatePowerBallFrequency", powerballFrequency);
        }

        public void UpdateBallFrequency(int[] ballFrequency, int amountOfNumbers, bool includeBonusBall, string lottoTypeWhere)
        {
            for (int i = 0; i < ballFrequency.Length; i++)
            {
                ballFrequency[i] = 0;
            }
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                List<string> nums = new List<string>();
                for (int i = 0; i < amountOfNumbers; i++)
                {
                    nums.Add($"\"num_{i + 1}\"");
                }
                if (includeBonusBall)
                {
                    nums.Add($"\"num_e\"");
                }
                foreach (var num in nums)
                {
                    string query = $"SELECT {num} AS nums, COUNT(*) FROM \"lotto_results\" WHERE {lottoTypeWhere} GROUP BY nums";
                    using (NpgsqlCommand com = new NpgsqlCommand(query, _con))
                    {
                        using (NpgsqlDataReader rdr = com.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                int currentball = rdr.GetInt32(0) - 1;
                                ballFrequency[currentball] += (int)rdr.GetInt64(1);
                            }
                        }
                    }
                }
            }
            _logger.Information("[{0}] [{1}]", "DatabaseContext.UpdateBallFrequency", ballFrequency);
        }

        public void UpdateHighs(int[] highs, int highValue, int amountOfNumbers, bool includeBonusBall, string lottoTypeWhere)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                List<string> nums = new List<string>();
                for (int i = 0; i < amountOfNumbers; i++)
                {
                    nums.Add($"(\"num_{i + 1}\" / {highValue})");
                }
                if (includeBonusBall)
                {
                    nums.Add($"(\"num_e\" / {highValue})");
                }
                string query = $"SELECT ({string.Join(" + ", nums)}) AS highs, COUNT(*) FROM \"lotto_results\" WHERE {lottoTypeWhere} GROUP BY highs";
                using (NpgsqlCommand com = new NpgsqlCommand(query, _con))
                {
                    using (NpgsqlDataReader rdr = com.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            highs[rdr.GetInt32(0)] = (int)rdr.GetInt64(1);
                        }
                    }
                }
            }
            _logger.Information("[{0}] [{1}]", "DatabaseContext.UpdateHighs", highs);
        }

        public void UpdateOdds(int[] odds, int amountOfNumbers, bool includeBonusBall, string lottoTypeWhere)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                }
                List<string> nums = new List<string>();
                for (int i = 0; i < amountOfNumbers; i++)
                {
                    nums.Add($"(\"num_{i + 1}\" % 2)");
                }
                if (includeBonusBall)
                {
                    nums.Add("(\"num_e\" % 2)");
                }
                string query = $"SELECT ({string.Join(" + ", nums)}) AS odds, COUNT(*) FROM \"lotto_results\" WHERE {lottoTypeWhere} GROUP BY odds";
                using (NpgsqlCommand com = new NpgsqlCommand(query, _con))
                {
                    using (NpgsqlDataReader rdr = com.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            odds[rdr.GetInt32(0)] = (int)rdr.GetInt64(1);
                        }
                    }
                }
            }
            _logger.Information("[{0}] [{1}]", "DatabaseContext.UpdateOdds", odds);
        }

        public DateTime GetLastResultDate()
        {
            DateTime returnValue = DateTime.Parse(Config.configVaraibles.defaultStartTime);
            lock (_connectionLock)
            {
                try
                {
                    while (TestConnection() == false)
                    {
                        _logger.Warning("[{0}] [{1}]", "DatabaseContext.InsertLottoResults", "Reconnecting...");
                    }
                    string query = "SELECT MAX(\"timestamp\") FROM \"lotto_results\"";
                    using (NpgsqlCommand com = new NpgsqlCommand(query, _con))
                    {
                        DateTime tempValue = (DateTime)com.ExecuteScalar();
                        if (tempValue > returnValue)
                        {
                            returnValue = tempValue;
                        }
                    }
                }
                catch (Exception err)
                {
                    _logger.Error(err, "[{0}]", "DatabaseContext.GetLastResultDate");
                }

            }
            _logger.Information("[{0}] [{1}]", "DatabaseContext.GetLastResultDate", returnValue);
            return returnValue;
        }

        private bool TestConnection()
        {
            if (_con.State != System.Data.ConnectionState.Open)
            {
                try
                {
                    _con.OpenAsync().Wait();
                }
                catch (Exception err)
                {
                    _logger.Error(err, "[{0}]", "DatabaseContext.TestConnection");
                    Thread.Sleep(1234);
                    return false;
                }
            }
            return true;
        }
    }
}
