namespace SnakesMaster.API.DTOs.User
{
    public class CreateUserResponse
    {
        public Guid PublicIdentifier { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Message { get; set; } // Added message field
    }
}
