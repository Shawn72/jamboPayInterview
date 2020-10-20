namespace JamboPayInterview.Models
{
    public class TransactionsModel
    {
        public string supporter_id { get; set; }
        public string ambassador_id { get; set; }
        public decimal transaction_cost { get; set; }
        public decimal ambassador_commission { get; set; }
    }
}