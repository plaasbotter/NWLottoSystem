using static NWLottoSystem.Utils.Enums;

namespace NWLottoSystem.Models
{
    public class LottoEntry
    {
        public int id { get; set; }
        public LottoGames games { get; set; }
        public List<List<short>> numbers { get; set; }
        public DateTime timestamp { get; set; }
        public bool isChecked { get; set; }
        public string reference { get; set; }
        public int sender_id { get; set; }
        public LottoEntry()
        {
            numbers = new List<List<short>>();
        }
    }
}
