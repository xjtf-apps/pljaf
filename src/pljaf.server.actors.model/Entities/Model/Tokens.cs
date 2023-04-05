namespace pljaf.server.model;

[GenerateSerializer]
public class Tokens
{
    [Id(0)] public string AccessToken { get; set; }
    [Id(1)] public string RefreshToken { get; set; }
}