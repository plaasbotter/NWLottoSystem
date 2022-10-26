using NWLottoSystem.Utils;

namespace NWLottoSystem.Models.DatabaseMigrationModels
{
    public class lotto_results
    {
        [PrimaryKey]
        public DateTime timestamp { get; set; }
        public short num_1 { get; set; }
        public short num_2 { get; set; }
        public short num_3 { get; set; }
        public short num_4 { get; set; }
        public short num_5 { get; set; }
        public short num_6 { get; set; }
        public short num_e { get; set; }
        [PrimaryKey]
        public short lotto_type { get; set; }
    }
}
