using SnakesMaster.API.Models;

public class CreateHighScoreResponse
{
    public int Score { get; set; }
    public User ScoredBy { get; set; }
    public DateTime ScoredOn { get; set; }
}