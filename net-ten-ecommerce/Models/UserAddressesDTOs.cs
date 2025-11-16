namespace net_ten_ecommerce.Models;

public class CreateAddressRequest
{
    public Address Address { get; set; } = new();
    public string Label { get; set; } = "Ev";
    public AddressType AddressType { get; set; } = AddressType.Home;
    public bool IsDefaultShipping { get; set; }
    public bool IsDefaultBilling { get; set; }
}

public class UpdateAddressRequest
{
    public Address? Address { get; set; }
    public string? Label { get; set; }
    public AddressType? AddressType { get; set; }
    public bool? IsDefaultShipping { get; set; }
    public bool? IsDefaultBilling { get; set; }
}