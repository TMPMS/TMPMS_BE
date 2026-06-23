namespace TMPMS.DTOs
{
    public class AccountRequestDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class AccountResponseDTO
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public string AccountId { get; set; }
    }
}
