using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnakesMaster.API.Data;
using SnakesMaster.API.Models;

namespace SnakesMaster.API.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ScoreController : ControllerBase
    {
        private readonly ApplicationDBContext _appDbContext;

        public ScoreController(ApplicationDBContext appDbContext)
        {
            _appDbContext = appDbContext;
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
        public async Task<ActionResult> CreateScore([FromBody] int scoreValue, CancellationToken cancellationToken)
        {
            if (scoreValue < 0)
                return BadRequest("Score must be greater than 0.");

            var auth0Identifier = HttpContext.Items["Auth0Identifier"] as string;

            if (string.IsNullOrEmpty(auth0Identifier))
                return Unauthorized("Auth0Identifier is missing or invalid.");

            var user = await _appDbContext.Users
                .Where(u => u.Auth0Identifier == auth0Identifier && u.IsActive && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
                return BadRequest("The user does not exist or is inactive.");

            var newScore = new HighScore
            {
                PublicIdentifier = Guid.NewGuid(),
                Score = scoreValue,
                DateAchieved = DateTime.UtcNow,
                ScoredBy = user.PublicIdentifier
            };

            _appDbContext.HighScores.Add(newScore);
            await _appDbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetScoreById), new { publicIdentifier = newScore.PublicIdentifier }, newScore);
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
    }
}
