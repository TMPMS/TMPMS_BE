namespace TMPMS.DTOs
{
    public class AddressDto
    {
        public string AddressLine { get; set; }

        public string City { get; set; }

        public string District { get; set; }

        public string Ward { get; set; }

        public bool IsDefault { get; set; }
    }
}
