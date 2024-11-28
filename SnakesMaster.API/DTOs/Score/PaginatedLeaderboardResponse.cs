namespace SnakesMaster.API.DTOs.HighScore
{
    public class PaginatedLeaderboardResponse
    {
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public List<LeaderboardItemResponse> Leaderboard { get; set; }
    }

    public class LeaderboardItemResponse
    {
        public Guid PublicIdentifier { get; set; }
        public int Score { get; set; }
        public DateTime DateAchieved { get; set; }
        public string Name { get; set; }
        public string ProfilePicUrl { get; set; }
        public string Rank { get; set; }
    }
}
