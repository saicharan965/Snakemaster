using SnakesMaster.API.Models;

public class CreateHighScoreRequest
{
    public int Score { get; set; }

    public Guid ScoredBy { get; set; }
}