namespace NWLottoSystem.Models
{
    public class LottoAPIResultsModel
    {
        public int code { get; set; }
        public string message { get; set; }
        public LottoDatum[] data { get; set; }
    }

    public class LottoDatum
    {
        public string drawNumber { get; set; }
        public string drawDate { get; set; }
        public string ball1 { get; set; }
        public string ball2 { get; set; }
        public string ball3 { get; set; }
        public string ball4 { get; set; }
        public string ball5 { get; set; }
        public string ball6 { get; set; }
        public string bonusBall { get; set; }
    }

}
