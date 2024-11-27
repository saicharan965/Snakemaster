using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnakesMaster.API.Data;
using SnakesMaster.API.Models;

namespace SnakesMaster.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HighScoreController : ControllerBase
    {
        private readonly ApplicationDBContext _appDbContext;

        public HighScoreController(ApplicationDBContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetHighScoresResponse>>> GetHighScores(
            CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
            {
                return BadRequest("Page and PageSize must be greater than 0.");
            }

            var highScores = await _appDbContext.HighScores
                .OrderByDescending(h => h.Score)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new GetHighScoresResponse
                {
                    PublicIdentifier = h.PublicIdentifier,
                    Score = h.Score,
                    DateAchieved = h.DateAchieved,
                    ScoredBy = _appDbContext.Users
                        .Where(u => u.PublicIdentifier == h.ScoredBy && u.IsActive && !u.IsDeleted)
                        .Select(u => new User
                        {
                            PublicIdentifier = u.PublicIdentifier,
                            FirstName = u.FirstName,
                            LastName = u.LastName,
                        })
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return Ok(highScores);
        }

        [HttpGet("{publicIdentifier:guid}")]
        public async Task<ActionResult<GetHighScoresResponse>> GetHighScoreById(Guid publicIdentifier, CancellationToken cancellationToken)
        {
            var highScore = await _appDbContext.HighScores
                .Where(h => h.PublicIdentifier == publicIdentifier)
                .Select(h => new GetHighScoresResponse
                {
                    PublicIdentifier = h.PublicIdentifier,
                    Score = h.Score,
                    DateAchieved = h.DateAchieved,
                    ScoredBy = _appDbContext.Users
                        .Where(u => u.PublicIdentifier == h.ScoredBy && u.IsActive && !u.IsDeleted)
                        .Select(u => new User
                        {
                            PublicIdentifier = u.PublicIdentifier,
                            FirstName = u.FirstName,
                            LastName = u.LastName,
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (highScore == null)
                return NotFound($"High score with ID {publicIdentifier} was not found.");

            return Ok(highScore);
        }

        [HttpPost]
        public async Task<ActionResult<CreateHighScoreResponse>> CreateHighScore(
            [FromBody] CreateHighScoreRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest("Request cannot be null.");

            var user = await _appDbContext.Users
                .Where(u => u.PublicIdentifier == request.ScoredBy && u.IsActive && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
                return BadRequest("The user does not exist or is inactive.");

            var newHighScore = new HighScore
            {
                PublicIdentifier = Guid.NewGuid(),
                Score = request.Score,
                DateAchieved = DateTime.UtcNow,
                ScoredBy = request.ScoredBy,
            };

            _appDbContext.HighScores.Add(newHighScore);
            await _appDbContext.SaveChangesAsync(cancellationToken);

            var response = new CreateHighScoreResponse
            {
                Score = newHighScore.Score,
                ScoredBy = user,
                ScoredOn = newHighScore.DateAchieved
            };

            return CreatedAtAction(nameof(GetHighScoreById), new { publicIdentifier = newHighScore.PublicIdentifier }, response);
        }

        [HttpPut("{publicIdentifier:guid}")]
        public async Task<ActionResult> UpdateHighScore(Guid publicIdentifier, [FromBody] CreateHighScoreRequest request, CancellationToken cancellationToken)
        {
            var highScoreToUpdate = await _appDbContext.HighScores
                .FirstOrDefaultAsync(h => h.PublicIdentifier == publicIdentifier, cancellationToken);

            if (highScoreToUpdate == null)
                return NotFound($"High score with ID {publicIdentifier} was not found.");

            highScoreToUpdate.Score = request.Score;
            highScoreToUpdate.DateAchieved = DateTime.UtcNow;

            await _appDbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{publicIdentifier:guid}")]
        public async Task<ActionResult> DeleteHighScore(Guid publicIdentifier, CancellationToken cancellationToken)
        {
            var highScoreToDelete = await _appDbContext.HighScores
                .FirstOrDefaultAsync(h => h.PublicIdentifier == publicIdentifier, cancellationToken);

            if (highScoreToDelete == null)
                return NotFound($"High score with ID {publicIdentifier} was not found.");

            _appDbContext.HighScores.Remove(highScoreToDelete);
            await _appDbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
