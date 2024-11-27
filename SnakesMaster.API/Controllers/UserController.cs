using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnakesMaster.API.Data;
using SnakesMaster.API.DTOs.User;
using SnakesMaster.API.Models;

namespace SnakesMaster.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDBContext _appDbContext;

        public UserController(ApplicationDBContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CreateUserResponse>> CreateOrGetUser([FromBody] CreateUserRequest userDetails, CancellationToken cancellationToken)
        {
            if (userDetails == null)
            {
                return BadRequest("User details cannot be null.");
            }

            var auth0Identifier = HttpContext.Items["Auth0Identifier"] as string;
            if (string.IsNullOrEmpty(auth0Identifier))
            {
                return Unauthorized("Auth0 Identifier is missing from the request context.");
            }

            var user = await _appDbContext.Users
                .FirstOrDefaultAsync(u => u.Auth0Identifier == auth0Identifier, cancellationToken);

            if (user != null)
            {
                return Ok(new CreateUserResponse
                {
                    PublicIdentifier = user.PublicIdentifier,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    EmailId = user.EmailId,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Message = "User fetched successfully."
                });
            }

            var newUser = new User
            {
                Auth0Identifier = auth0Identifier,
                PublicIdentifier = Guid.NewGuid(),
                FirstName = userDetails.FirstName,
                LastName = userDetails.LastName,
                EmailId = userDetails.EmailId,
                ProfilePictureUrl = userDetails.ProfilePictureUrl,
                IsDeleted = false,
                IsActive = true
            };

            _appDbContext.Users.Add(newUser);
            await _appDbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetUserById), new { id = newUser.PublicIdentifier }, new CreateUserResponse
            {
                PublicIdentifier = newUser.PublicIdentifier,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                EmailId = newUser.EmailId,
                ProfilePictureUrl = newUser.ProfilePictureUrl,
                Message = "User created successfully."
            });
        }

        [HttpPut]
        public async Task<ActionResult<UserDetailsResponse>> UpdateUser([FromBody] UpdateUserRequest userDetails, CancellationToken cancellationToken)
        {
            var user = await _appDbContext.Users
                .FirstOrDefaultAsync(u => u.PublicIdentifier == userDetails.PublicIdentifier, cancellationToken);

            if (user == null)
            {
                return NotFound($"The user with ID {userDetails.PublicIdentifier} was not found.");
            }

            user.ProfilePictureUrl = userDetails.ProfilePictureUrl;
            user.FirstName = userDetails.FirstName;
            user.LastName = userDetails.LastName;
            user.EmailId = userDetails.EmailId;

            await _appDbContext.SaveChangesAsync(cancellationToken);

            return Ok(MapToUserDetailsResponse(user));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteUser([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("User Id cannot be empty GUID.");
            }

            var user = await _appDbContext.Users
                .FirstOrDefaultAsync(u => u.PublicIdentifier == id, cancellationToken);

            if (user == null)
            {
                return NotFound($"The user with ID {id} was not found.");
            }

            user.IsDeleted = true;
            await _appDbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserDetailsResponse>> GetUserById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var user = await _appDbContext.Users
                .FirstOrDefaultAsync(u => u.PublicIdentifier == id, cancellationToken);

            if (user == null)
            {
                return NotFound($"User with ID {id} was not found.");
            }

            return Ok(MapToUserDetailsResponse(user));
        }

        private UserDetailsResponse MapToUserDetailsResponse(User user)
        {
            return new UserDetailsResponse
            {
                PublicIdentifier = user.PublicIdentifier,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailId = user.EmailId,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsDeleted = user.IsDeleted,
                IsActive = user.IsActive
            };
        }
    }
}
