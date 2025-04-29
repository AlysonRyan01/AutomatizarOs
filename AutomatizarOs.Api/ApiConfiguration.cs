namespace AutomatizarOs.Api;

public class ApiConfiguration
{
    public static string CorsPolicyName = "CorsApi";
    public static string JwtKey { get; set; } = string.Empty;
    public static string Key => "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";
}