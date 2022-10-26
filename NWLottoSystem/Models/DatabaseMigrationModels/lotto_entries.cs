namespace NWLottoSystem.Models.DatabaseMigrationModels
{
    internal class lotto_entries
    {
        public int id { get; set; }
        public short num_1 { get; set; }
        public short num_2 { get; set; }
        public short num_3 { get; set; }
        public short num_4 { get; set; }
        public short num_5 { get; set; }
        public short num_6 { get; set; }
        public short games { get; set; }
        public bool Checked { get; set; }
        public string reference { get; set; }
        public int sender_id { get; set; }
        public DateTime timestamp { get; set; }
    }
}
