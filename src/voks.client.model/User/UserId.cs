namespace voks.client.model;

public record class UserId(string PhoneNumber);

public record class UserIdPlus(string PhoneNumber, string? DisplayName) : UserId(PhoneNumber);
