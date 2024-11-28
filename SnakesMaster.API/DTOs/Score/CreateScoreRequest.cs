using SnakesMaster.API.Models;

public class CreateScoreRequest
{
    public int Score { get; set; }
    public User ScoredBy { get; set; }
    public DateTime ScoredOn { get; set; }
}