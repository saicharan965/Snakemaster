using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnakesMaster.API.Data;
using SnakesMaster.API.DTOs.HighScore;
using SnakesMaster.API.Models;

namespace SnakesMaster.API.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ScoreController : ControllerBase
    {
        private readonly ApplicationDBContext _appDbContext;
        private readonly ILogger<ScoreController> _logger;

        public ScoreController(ApplicationDBContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedLeaderboardResponse>> GetLeaderboard(CancellationToken cancellationToken,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            var skip = (page - 1) * pageSize;
            var leaderboardQuery = _appDbContext.HighScores
                .Where(h => h.Score > 0)
                .GroupBy(h => new { h.ScoredBy, h.Score })
                .Select(group => new
                {
                    group.Key.ScoredBy,
                    group.Key.Score,
                    DateAchieved = group.Max(h => h.DateAchieved)
                })
                .OrderByDescending(h => h.Score)
                .ThenByDescending(h => h.DateAchieved)
                .Skip(skip)
                .Take(pageSize);

            var leaderboard = await leaderboardQuery
                .Select(h => new
                {
                    h.ScoredBy,
                    h.Score,
                    h.DateAchieved,
                    ScoredByDetails = _appDbContext.Users
                        .Where(u => u.PublicIdentifier == h.ScoredBy && u.IsActive && !u.IsDeleted)
                        .Select(u => new
                        {
                            u.PublicIdentifier,
                            u.FirstName,
                            u.LastName,
                            u.ProfilePictureUrl
                        })
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            var totalScores = await _appDbContext.HighScores
                .Where(h => h.Score > 0)
                .GroupBy(h => new { h.ScoredBy, h.Score })
                .CountAsync(cancellationToken);

            var leaderboardWithRank = leaderboard
                .Select((item, index) => new LeaderboardItemResponse
                {
                    PublicIdentifier = item.ScoredBy,
                    Score = item.Score,
                    DateAchieved = item.DateAchieved,
                    Name = $"{item.ScoredByDetails?.FirstName} {item.ScoredByDetails?.LastName}",
                    ProfilePicUrl = item.ScoredByDetails?.ProfilePictureUrl,
                    Rank = GetOrdinal(index + 1)
                })
                .ToList();

            var totalPages = (int)Math.Ceiling((double)totalScores / pageSize);

            var response = new PaginatedLeaderboardResponse
            {
                TotalPages = totalPages,
                CurrentPage = page,
                Leaderboard = leaderboardWithRank
            };

            return Ok(response);
        }

        [HttpGet("{publicIdentifier:guid}")]
        public async Task<ActionResult<GetHighScoreResponse>> GetScoreById(Guid publicIdentifier, CancellationToken cancellationToken)
        {
            var score = await _appDbContext.HighScores
                .Where(h => h.PublicIdentifier == publicIdentifier)
                .Select(h => new
                {
                    h.PublicIdentifier,
                    h.Score,
                    h.DateAchieved,
                    ScoredBy = _appDbContext.Users
                        .Where(u => u.PublicIdentifier == h.ScoredBy && u.IsActive && !u.IsDeleted)
                        .Select(u => new
                        {
                            u.PublicIdentifier,
                            u.FirstName,
                            u.LastName
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (score == null)
                return NotFound($"Score with ID {publicIdentifier} was not found.");

            return Ok(score);
        }

        [HttpPost]
        public async Task<ActionResult<CreateScoreResponse>> CreateScore([FromBody] int scoreValue, CancellationToken cancellationToken)
        {
            if (scoreValue < 0)
            {
                return BadRequest("Score must be greater than or equal to 0.");
            }
            var auth0Identifier = HttpContext.Items["Auth0Identifier"] as string;
            if (string.IsNullOrEmpty(auth0Identifier))
            {
                return Unauthorized("Auth0 identifier is missing or invalid.");
            }
            var user = await _appDbContext.Users
                .Where(u => u.Auth0Identifier == auth0Identifier && u.IsActive && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
            if (user == null)
            {
                return BadRequest("The user does not exist or is inactive.");
            }
            var existingScore = await _appDbContext.HighScores
                .Where(h => h.ScoredBy == user.PublicIdentifier && h.Score == scoreValue)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingScore != null)
            {
                existingScore.DateAchieved = DateTime.UtcNow;
                _appDbContext.HighScores.Update(existingScore);
            }
            else
            {
                var newScore = new HighScore
                {
                    PublicIdentifier = Guid.NewGuid(),
                    Score = scoreValue,
                    DateAchieved = DateTime.UtcNow,
                    ScoredBy = user.PublicIdentifier
                };
                _appDbContext.HighScores.Add(newScore);
            }
            try
            {
                await _appDbContext.SaveChangesAsync(cancellationToken);
                var rank = await GetScoreRank(scoreValue, cancellationToken);

                var result = new CreateScoreResponse
                {
                    Score = scoreValue,
                    Rank = GetOrdinal(rank)
                };

                return CreatedAtAction(nameof(GetScoreById), new { publicIdentifier = existingScore?.PublicIdentifier }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the score.");
                return StatusCode(500, "An error occurred while saving the score.");
            }
        }

        [HttpDelete("{publicIdentifier:guid}")]
        public async Task<ActionResult> DeleteScore(Guid publicIdentifier, CancellationToken cancellationToken)
        {
            var scoreToDelete = await _appDbContext.HighScores
                .FirstOrDefaultAsync(h => h.PublicIdentifier == publicIdentifier, cancellationToken);

            if (scoreToDelete == null)
                return NotFound($"Score with ID {publicIdentifier} was not found.");

            _appDbContext.HighScores.Remove(scoreToDelete);
            await _appDbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        private async Task<int> GetScoreRank(int scoreValue, CancellationToken cancellationToken)
        {
            var leaderboard = await _appDbContext.HighScores
                .Where(h => h.Score >= scoreValue)
                .OrderByDescending(h => h.Score)
                .ToListAsync(cancellationToken);
            var rank = leaderboard.FindIndex(h => h.Score == scoreValue) + 1;
            return rank;
        }

        private string GetOrdinal(int number)
        {
            if (number <= 0)
                return number.ToString();

            int ones = number % 10;
            int tens = (number % 100) / 10;

            if (tens == 1)
                return number + "th";

            switch (ones)
            {
                case 1: return number + "st";
                case 2: return number + "nd";
                case 3: return number + "rd";
                default: return number + "th";
            }
        }
    }
}

