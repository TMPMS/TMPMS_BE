namespace TMPMS.DTOs
{
    public class SupplierDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string company_name => CompanyName;
        public string ContactPerson { get; set; } = string.Empty;
        public string contact_person => ContactPerson;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string tax_code => TaxCode;
        public string Status { get; set; } = "Active";
    }

    public class SupplierCreateDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }
}
