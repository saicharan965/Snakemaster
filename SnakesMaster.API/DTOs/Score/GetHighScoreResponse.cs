﻿using SnakesMaster.API.Models;

public class GetHighScoreResponse
{
    public Guid PublicIdentifier { get; set; }
    public int Score { get; set; }
    public DateTime DateAchieved { get; set; }

    public User ScoredBy { get; set; }
}