namespace SnakesMaster.API.Models
{
    public class HighScore
    {
        public int HighScoreId { get; set; }
        public Guid PublicIdentifier { get; set; }
        public int UserId { get; set; }
        public int Score { get; set; }
        public DateTime DateAchieved { get; set; }

        public Guid ScoredBy { get; set; }
    }
}
