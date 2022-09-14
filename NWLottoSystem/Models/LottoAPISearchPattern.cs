using static NWLottoSystem.Utils.Enums;

namespace NWLottoSystem.Models
{
    public class LottoAPISearchPattern
    {
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public LottoGames lottoGame { get; set; }
        public int results { get; set; }
    }
}
